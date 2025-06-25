
using System.Text;
using System.Collections.Generic;

using static LibCPU.RISCV;
using System.Runtime.InteropServices;
//using System.IO.Pipelines;
using System.Formats.Asn1;
using System.Security;
using System.Data;
using System.Transactions;
using System.Numerics;
namespace LibCPU {
    // circular queue used for ROB and LSbuffer in OOO CPU
    public class CircularQueue<T> {
        public int start, end, size;
        private T[] elements;

        public CircularQueue(int initialSize) {
            this.start = this.end = -1;
            this.size = initialSize;
            this.elements = new T[initialSize];
        }

        public int enqueue(T element) {
            if(isFull()) return -2;
            else if (isEmpty()) start++;

            end = (end + 1) % size;
            elements[end] = element;
            return end;
        }

        public ref T dequeue() {
            if(isEmpty())
                throw new Exception($"Queue is empty");
            ref T temp = ref elements[start];
            if(start == end) start = end = -1;
            else start = (start + 1) % size;

            return ref temp;
        }

        public ref T getIndex(int index) { 
            if(isEmpty())
                throw new Exception($"Queue is empty");

            if(index < size && index >= 0)
                if(start <= end && index >=start && index <= end)
                    return ref elements[index];
                else if(start > end && index >= start || index <= end)
                    return ref elements[index];
                else
                    throw new Exception($"Invalid Index");      
            else
                throw new Exception($"Invalid Index");      
        }

        public bool isEmpty() { return start == -1; }
        public bool isFull() { return ((end + 1) % 16) == start; }
        public void flush() { start = end = -1; }
    }   

    public enum CPU_type
    {
        PipeLined, SingleCycle, OOO
    }
    public static class RISCV
    {

        public static int MAX_CLOCKS = 100 * 1000 * 1000;
        public static int HANDLER_ADDR = 1000;
        public static int IM_SIZE;
        public static int DM_SIZE;


        public const string EXCEPTION = "EXCEPTION";
        public const string FETCH = "FETCH";
        public const string DECODE = "DECODE";
        public const string EXECUTE = "EXECUTE";
        public const string MEMORY = "MEMORY";
        public const string WRITEBACK = "WRITEBACK";
        public const string INVALID_OPCODED = "INVALID_OPCODED";
        public const string BUBBLE = "BUBBLE";
        public const string WRONG_PRED = "WRONG_PRED";
        public const string LOAD_USE = "LOAD_USE";
        public const string JR_INDECODE = "JR_INDECODE";
        public const string JR_INEX1 = "JR_INEX1";
        public enum Mnemonic
        {
            add, addu, subu, sub, and, or, nor, slt, seq, sne, sgt, xor,
            addi, andi, ori, xori, slti, sll, srl, mult,
            beq, bne,
            j, jr, jal, 
            lw, sw,
            nop, hlt
        }
        public enum Aluop
        {
            add, sub, and, or, xor, nor, sll, srl, slt, seq, sne, sgt, div, mult
        }
        public struct Instruction
        {
            public Instruction Init()
            {
                mc = "";
                mc = mc.PadLeft(32, '0');
                opcode = "000000";
                shamt = "00000";
                format = "R";
                funct = "000000";
                mnem = Mnemonic.nop;
                aluop = 0;
                rsind = 0;
                rtind = 0;
                rdind = 0;
                address = 0;
                immeds = 0;
                immedz = 0;
                PC = 0;
                rs = 0;
                rt = 0;
                oper1 = 0;
                oper2 = 0;
                aluout = 0;
                memout = 0;
                prediction = false;
                valid = true;
                return this;
            }
            public string mc;
            public string opcode;
            public int rsind;
            public int rtind;
            public int rdind;
            public string shamt;
            public string funct;
            public int address;
            public int immeds;
            public int immedz;

            public Mnemonic mnem;
            public int PC;
            public int rs;
            public int rt;
            public int oper1;
            public int oper2;
            public Aluop aluop;
            public string format;
            public int aluout;
            public int memout;
            public bool prediction;
            public bool valid;
        }
            
        public static Instruction GetNewInst()
        {
            return new Instruction().Init();
        }

        public static (List<string>, List<string>, List<int>) InitMipsCPU(List<string> insts, List<string> data_mem_init)
        {
            List<int> regs = new List<int>();
            for (int i = 0; i < 32; i++) regs.Add(0);
            List<string> IM = new List<string>();
            int curr_count = 0;
            if (insts != null)
            {
                IM.AddRange(insts);
                curr_count = insts.Count;
            }
            string nop = "0".PadLeft(32, '0');
            for (int i = 0; i < IM_SIZE - curr_count; i++) IM.Add(nop);

            //IM[HANDLER_ADDR - 1] = "11111100000000000000000000000000"; // hlt
            //IM[HANDLER_ADDR] = "00100000000111111111111111111111"; // addi $31 $0 -1
            //IM[HANDLER_ADDR + 1] = "11111100000000000000000000000000"; // hlt

            List<string> DM = [.. data_mem_init];

            for (int i = 0; i < DM_SIZE - data_mem_init.Count; i++) DM.Add("0");

            return (IM, DM, regs);
        }

        public struct CPU
        {
            public List<string> DM;
            public List<int> regs;

            public CPU Init()
            {
                DM = new List<string>();
                regs = new List<int>();
                return this;
            }

        }

        public enum Stage { fetch, decode, execute1, execute2, memory, write_back }
        public enum Exceptions { NONE, INF_LOOP, INVALID_INST, EXCEPTION }

        public static readonly Dictionary<string, Mnemonic> mnemonicmap = new Dictionary<string, Mnemonic>() {
            // the nop is not mentioned here to not conflict with sll
            // R-format depends on the funct field, if opcode = "000000" then it is R-format else (it is an I-format or J-format either way it depends on distinct opcodes)
            // rd = rd, rs1 = rs, rs2 = rt
            { "000000100000" , Mnemonic.add  }, // R[rd] = R[rs] op R[rt]
            { "000000101000" , Mnemonic.mult  }, // R[rd] = R[rs] op R[rt]
            { "000000100001" , Mnemonic.addu }, // R[rd] = R[rs] op R[rt]
            { "000000100010" , Mnemonic.sub  }, // R[rd] = R[rs] op R[rt]
            { "000000100011" , Mnemonic.subu }, // R[rd] = R[rs] op R[rt]
            { "000000100100" , Mnemonic.and  }, // R[rd] = R[rs] op R[rt]
            { "000000100101" , Mnemonic.or   }, // R[rd] = R[rs] op R[rt]
            { "000000100110" , Mnemonic.xor  }, // R[rd] = R[rs] op R[rt]
            { "000000100111" , Mnemonic.nor  }, // R[rd] = R[rs] op R[rt]
            { "000000101010" , Mnemonic.slt  }, // R[rd] = R[rs] op R[rt]
            { "000000101100" , Mnemonic.seq  }, // R[rd] = R[rs] op R[rt]
            { "000000101101" , Mnemonic.sne  }, // R[rd] = R[rs] op R[rt]
            { "000000101011" , Mnemonic.sgt  }, // R[rd] = R[rs] op R[rt]
            { "000000000000" , Mnemonic.sll  }, // R[rd] = R[rt] op shamt
            { "000000000010" , Mnemonic.srl  }, // R[rd] = R[rt] op shamt
            { "000000001000" , Mnemonic.jr   }, // PC = R[rs] (here we jump to the instruciont in the IM addressed by R[rs])

            // I-format depends on the opcode field
            // rd = rt, rs1 = rs, rs2 = rt, immed = immed or addr
            { "001000000000" , Mnemonic.addi }, // R[rt] = R[rs] op sx(immed)  
            { "001100000000" , Mnemonic.andi }, // R[rt] = R[rs] op zx(immed)  
            { "001101000000" , Mnemonic.ori  }, // R[rt] = R[rs] op zx(immed)  
            { "001110000000" , Mnemonic.xori }, // R[rt] = R[rs] op zx(immed)  
            { "101010000000" , Mnemonic.slti }, // R[rt] = R[rs] op sx(immed)  
            { "100011000000" , Mnemonic.lw   }, // R[rt] = Mem[R[rs]+sx(immed)]
            { "101011000000" , Mnemonic.sw   }, // Mem[R[rs]+sx(immed)]=R[rt]  
            // rs1 = rs, rs2 = rt
            { "000100000000" , Mnemonic.beq  }, // if (R[rs] op R[rt]) -> PC += sx(offset) << 2  , note.1
            { "000101000000" , Mnemonic.bne  }, // if (R[rs] op R[rt]) -> PC += sx(offset) << 2  , note.1

            // J-format depends on opcode field
            { "000010000000" , Mnemonic.j    }, // PC = zx(addr) << 2  , note.1
            { "000011000000" , Mnemonic.jal  }, // R[31] = PC+1, PC = zx(addr) << 2  , note.1


            { "111111000000" , Mnemonic.hlt  },
        };
        // note.1 : apparently the shift left by 2 is optional because because the IM may be word addressable rather than byte addressable.
        // in other words if the IM was byte addressable each location holds one of four bytes of an instruction and when we do a branch for example the offset by it self
        // represents how many instruction is the target address away from it so we shift left by 2 (multiply by four) so we can account for each byte in the IM (given that each
        // instrution is four bytes and it is the case here)...
        // BUT if the IM is word addressable and the size of the word depends on the architecture in our case it's 32-bit (four bytes), we won't need to shift because each
        // location is alread four bytes and holding a whole instruction
        public static Aluop get_inst_aluop(Mnemonic mnem)
        {
        return mnem switch
        {
            Mnemonic.add => Aluop.add,
            Mnemonic.mult => Aluop.mult,
            Mnemonic.sub => Aluop.sub,
            Mnemonic.and => Aluop.and,
            Mnemonic.andi => Aluop.and,
            Mnemonic.or => Aluop.or,
            Mnemonic.ori => Aluop.or,
            Mnemonic.xor => Aluop.xor,
            Mnemonic.xori => Aluop.xor,
            Mnemonic.nor => Aluop.nor,
            Mnemonic.slt => Aluop.slt,
            Mnemonic.seq => Aluop.seq,
            Mnemonic.sne => Aluop.sne,
            Mnemonic.slti => Aluop.slt,
            Mnemonic.sgt => Aluop.sgt,
            Mnemonic.sll => Aluop.sll,
            Mnemonic.srl => Aluop.srl,
            Mnemonic.addi => Aluop.add,
            Mnemonic.addu => Aluop.add,
            Mnemonic.subu => Aluop.sub,
            Mnemonic.beq => Aluop.sub,
            Mnemonic.bne => Aluop.sub,
            Mnemonic.j => Aluop.add,
            Mnemonic.jr => Aluop.add,
            Mnemonic.jal => Aluop.add,
            Mnemonic.lw => Aluop.add,
            Mnemonic.sw => Aluop.add,
            _ => 0,
        };
        ;
        }
        public static string get_format(Mnemonic mnem)
        {
        return mnem switch
        {
            Mnemonic.add => "R",
            Mnemonic.mult => "R",
            Mnemonic.sub => "R",
            Mnemonic.and => "R",
            Mnemonic.andi => "I",
            Mnemonic.or => "R",
            Mnemonic.ori => "I",
            Mnemonic.xor => "R",
            Mnemonic.xori => "I",
            Mnemonic.nor => "R",
            Mnemonic.slt => "R",
            Mnemonic.seq => "R",
            Mnemonic.sne => "R",
            Mnemonic.sgt => "R",
            Mnemonic.slti => "I",
            Mnemonic.sll => "R",
            Mnemonic.srl => "R",
            Mnemonic.addi => "I",
            Mnemonic.addu => "R",
            Mnemonic.subu => "R",
            Mnemonic.beq => "I",
            Mnemonic.bne => "I",
            Mnemonic.j => "J",
            Mnemonic.jr => "R",
            Mnemonic.jal => "J",
            Mnemonic.lw => "I",
            Mnemonic.sw => "I",
            Mnemonic.hlt => "I",
            _ => "",
        };
        ;
        }
        public static int execute_inst(Instruction inst)
        {
            switch (inst.aluop)
            {
                case Aluop.add: return inst.oper1 + inst.oper2;
                case Aluop.mult: return inst.oper1 * inst.oper2;
                case Aluop.sub: return inst.oper1 - inst.oper2;
                case Aluop.and: return inst.oper1 & inst.oper2;
                case Aluop.or: return inst.oper1 | inst.oper2;
                case Aluop.xor: return inst.oper1 ^ inst.oper2;
                case Aluop.nor: return ~(inst.oper1 | inst.oper2);
                case Aluop.slt: return (inst.oper1 < inst.oper2) ? 1 : 0;
                case Aluop.seq: return (inst.oper1 == inst.oper2) ? 1 : 0;
                case Aluop.sne: return (inst.oper1 != inst.oper2) ? 1 : 0;
                case Aluop.sgt: return (inst.oper1 > inst.oper2) ? 1 : 0;
                case Aluop.sll: return inst.oper1 << inst.oper2;
                case Aluop.srl:
                    {
                        if (inst.oper2 == 0)
                            return inst.oper1;
                        return (inst.oper1 >>> inst.oper2);
                    };
                default: return 0;
            };
        }
        public static bool iswb(Mnemonic mnem)
        {
        return mnem switch
        {
            Mnemonic.add => true,
            Mnemonic.mult => true,
            Mnemonic.sub => true,
            Mnemonic.and => true,
            Mnemonic.andi => true,
            Mnemonic.or => true,
            Mnemonic.ori => true,
            Mnemonic.xor => true,
            Mnemonic.xori => true,
            Mnemonic.nor => true,
            Mnemonic.slt => true,
            Mnemonic.seq => true,
            Mnemonic.sne => true,
            Mnemonic.sgt => true,
            Mnemonic.slti => true,
            Mnemonic.sll => true,
            Mnemonic.srl => true,
            Mnemonic.addi => true,
            Mnemonic.addu => true,
            Mnemonic.subu => true,
            Mnemonic.beq => false,
            Mnemonic.bne => false,
            Mnemonic.j => false,
            Mnemonic.jr => false,
            Mnemonic.jal => true,
            Mnemonic.lw => true,
            Mnemonic.sw => false,
            _ => false,
        };
        ;
        }
        public static bool isbranch_taken(Instruction inst)
        {
            return (inst.mnem == Mnemonic.beq && inst.oper1 == inst.oper2) ||
                    (inst.mnem == Mnemonic.bne && inst.oper1 != inst.oper2);
        }
        public static bool isbranch(Mnemonic mnem)
        {
            return mnem == Mnemonic.beq ||
                    mnem == Mnemonic.bne;
        }
        public static int get_oper1(Instruction inst) {
            if (inst.format == "R")
            {
                if (inst.mnem == Mnemonic.sll || inst.mnem == Mnemonic.srl)
                    return inst.rt;
                return inst.rs;
            }
            if (inst.format == "I")
            {
                return inst.rs;
            }
            if (inst.format == "J")
                return inst.PC + 1;
            throw new Exception($"Invalid format provided : {inst.format}");
        }
        public static bool islogical(Mnemonic mnem)
        {
            return mnem == Mnemonic.andi || mnem == Mnemonic.ori || mnem == Mnemonic.xori;
        }
        public static int get_oper2(Instruction inst)
        {
            if (inst.format == "R")
            {
                if (inst.mnem == Mnemonic.sll || inst.mnem == Mnemonic.srl)
                    return Convert.ToInt32(zx(inst.shamt), 2);
                if (inst.mnem == Mnemonic.jr)
                    return 0;
                return inst.rt;
            }
            if (inst.format == "I")
            {
                if (islogical(inst.mnem))
                    return inst.immedz;
                else if (isbranch(inst.mnem))
                    return inst.rt;
                return inst.immeds;
            }
            if (inst.format == "J")
            {
                return 0;
            }
            throw new Exception($"Invalid format provided : {inst.format}");
        }
        public static string sx(string num) { return num.PadLeft(32, num[0]); }
        public static string zx(string num) { return num.PadLeft(32, '0'); }
        public static void print_regs(List<int> regs) {
            Console.Write("Register file content : \n");
            int i = 0;
            foreach (int n in regs)
            {
                string temp = $"index = {i++,10} , reg_out : signed = {n,10} , unsigned = {(uint)n,10}\n";
                Console.Write(temp);
            }
        }
        public static void print_DM(List<string> DM) {
            Console.Write("Data Memory Content : \n");
            int i = 0;
            foreach (string mem in DM)
            {
                string temp;
                temp = $"Mem[{i++,4}] = {mem,11}\n";

                Console.Write(temp);
            }
        }

        public static StringBuilder get_regs(List<int> regs) {
            StringBuilder sb = new StringBuilder();
            sb.Append("Register file content : \n");
            int i = 0;
            foreach (int n in regs)
            {
                string temp = $"index = {i++,10} , reg_out : signed = {n,11} , unsigned = {(uint)n,10}\n";
                sb.Append(temp);
            }
            return sb;
        }

        public static StringBuilder get_DM(List<string> DM) {
            StringBuilder sb = new StringBuilder();
            sb.Append("Data Memory Content : \n");
            int i = 0;
            foreach (string mem in DM)
            {
                string temp;

                temp = $"Mem[{i++,4}] = {mem,11}\n";

                sb.Append(temp);
            }
            return sb;
        }

        public static bool isvalid_format(string format) { return format == "R" || format == "I" || format == "J"; }
        public static bool is_in_range_inc(int x, int lo, int hi) { return lo <= x && x <= hi; }
        public static bool isvalid_reg_ind(Instruction inst) {
            return is_in_range_inc(inst.rsind, 0, 32) &&
                    is_in_range_inc(inst.rtind, 0, 32) &&
                    is_in_range_inc(inst.rdind, 0, 32);
        }
        public static bool isvalid_opcode_funct(Instruction inst) { return mnemonicmap.ContainsKey(inst.opcode + inst.funct); }

    }
    
    public class OOO {
        static public int PC;
        static public bool hlt;
        static public int targetaddress;
        static public int predictorState;

        public enum PCsrc { PCplus1, branchTarget, exception, none }
        public PCsrc pcsrc;

        public enum handlerAddress { overflow = 100, underflow = 110, invalidAddress = 120 } // t o d o: change these to the right ones

        public List<string> DM; // Data Mem
        public List<int> regs; // register file
        public List<int> regs_ROBENS; // register file entries ROBENs
        public List<string> IM; // Instruction Mem

        // ROB
        public bool flush;
        public int ROBsize;

        public class ROBregister {
            public Mnemonic type;
            public int Rd;
            public bool busy;
            public bool ready;
            public bool exception;
            public int writeData; 
            public int PC;
            public bool branchDecision;
            public bool branchPrediction;

            public void clear() {
                this.type        = Mnemonic.nop;
                this.Rd          = 0;
                this.busy        = false;
                this.ready       = false;
                this.exception   = false;
                this.writeData   = 0;
                this.PC          = 0;
                this.branchDecision = false;
                this.branchPrediction = false;
            }

            public ROBregister(Mnemonic type) {
                this.type        = type;      
                this.Rd          = 0;
                this.busy        = false;
                this.ready       = false;
                this.exception   = false;
                this.writeData   = 0;
                this.PC          = 0;
                this.branchDecision = false;
                this.branchPrediction = false;
            }
        }

        public CircularQueue<ROBregister> ROB;

        // Reservation Station
        static public bool RSfullFlag;
        static public int RSsize;

        public class RSregister {
            
            public Aluop ALUop;
            public int ROBEN;
            public int ROBEN1;
            public int ROBEN2;
            public int ROBEN1_val;
            public int ROBEN2_val;
            public int immediate;
            public bool busy;

            public RSregister(Aluop ALUop) {
                this.ALUop       = ALUop;
                this.ROBEN       = 0;
                this.ROBEN1      = 0;
                this.ROBEN2      = 0;
                this.ROBEN1_val  = 0;
                this.ROBEN2_val  = 0;
                this.immediate = 0;
                this.busy = false;
            }
        }

        public List<RSregister> reservationStation;

        // Load Store Buffer
        static public int LSsize;

        public class LSregister {
            public Mnemonic type;
            public bool ready;
            public bool busy;
            public int Rd;
            public int effectiveAddress;
            public int ROBEN;
            public int ROBEN1;
            public int ROBEN2;
            public int ROBEN1_val;
            public int ROBEN2_val;
            public int immediate;

            public void clear() {
                this.ready = false;
                this.busy = false;
                this.Rd = 0;
                this.effectiveAddress = 0;
                this.ROBEN = 0;
                this.ROBEN1 = 0;
                this.ROBEN2 = 0;
                this.ROBEN1_val = 0;
                this.ROBEN2_val = 0;
                this.immediate = 0;
            }

            public LSregister(Mnemonic type) {
                this.type = type;
                this.ready = false;
                this.busy = false;
                this.Rd = 0;
                this.effectiveAddress = 0;
                this.ROBEN = 0;
                this.ROBEN1 = 0;
                this.ROBEN2 = 0;
                this.ROBEN1_val = 0;
                this.ROBEN2_val = 0;
                this.immediate = 0;
            }
        }

        public CircularQueue<LSregister> LSbuffer;

        public OOO(List<string> insts, List<string> data_mem_init) {
            PC              = 0;
            hlt             = false;
            targetaddress   = 0;
            flush           = false;
            ROBsize         = 16;
            RSsize         = 16;
            LSsize         = 16;
            predictorState = 0;

    
            (IM, DM, regs) = InitMipsCPU(insts, data_mem_init);
            
            regs_ROBENS = new List<int>();
            for (int i = 0; i < 32; i++) 
                regs_ROBENS.Add(0);

            // initializing ROB 
            ROB = new CircularQueue<ROBregister>(ROBsize);

            // initializing reservation station
            reservationStation = new List<RSregister>();
            for (int i = 0; i < RSsize; i++) 
                reservationStation.Add(new RSregister(Aluop.add));
            
            RSfullFlag = false;

            // initializing the LS buffer
            LSbuffer = new CircularQueue<LSregister>(LSsize);
        }

        public string fetchInstruction() {
            string fetched = "";
            if (PC >= IM.Count) return fetched.PadLeft(32, '0');
            fetched = IM[PC];
            return fetched;
        }

        Instruction decodemc(string mc, int pc) {
            Instruction inst = GetNewInst();
            inst.mc = mc;
            inst.PC = pc;
            inst.opcode = mc.Substring(mc.Length - (1 + 31), 6);
            inst.rsind = Convert.ToInt32(mc.Substring(mc.Length - (1 + 25), 5), 2);
            inst.rtind = Convert.ToInt32(mc.Substring(mc.Length - (1 + 20), 5), 2);
            inst.rdind = Convert.ToInt32(mc.Substring(mc.Length - (1 + 15), 5), 2);
            inst.shamt = mc.Substring(mc.Length - (1 + 10), 5);
            inst.funct = mc.Substring(mc.Length - (1 + 5), 6);
            inst.immeds = Convert.ToInt32(sx(mc.Substring(mc.Length - (1 + 15), 16)), 2);
            inst.immedz = Convert.ToInt32(zx(mc.Substring(mc.Length - (1 + 15), 16)), 2);
            inst.address = Convert.ToInt32(zx(mc.Substring(mc.Length - (1 + 25), 26)), 2);

            // riscv integer instruction map for formats (R, I, J)
            inst.rs = regs[inst.rsind];
            inst.rt = regs[inst.rtind];

            if (inst.opcode != "000000")
                inst.funct = "000000";
            if (!mnemonicmap.TryGetValue(inst.opcode + inst.funct, out Mnemonic value))
            {
                Exception e = new Exception(EXCEPTION)
                {
                    Source = DECODE
                };
                throw e;
            }
            else
            {
                if (mc == "".PadLeft(32, '0'))
                    inst.mnem = Mnemonic.nop;
                inst.mnem = value;
            }
            inst.aluop = get_inst_aluop(inst.mnem);
            inst.format = get_format(inst.mnem);
            inst.oper1 = get_oper1(inst);
            inst.oper2 = get_oper2(inst);
            if (inst.format == "I")
            {
                inst.rdind = inst.rtind;
            }
            if (inst.format == "J")
            {
                inst.rdind = 31;
            }
            return inst;
        }

        // dispatch instruction from instruction queue
        public bool dispatch_RS_LS(Instruction currInstruction, bool prediction, int tempROBen) {
   
            if(currInstruction.mnem == Mnemonic.j || currInstruction.mnem == Mnemonic.jal) return true; 

            // handles all non-load and non-store instructions
            if(!(currInstruction.mnem == Mnemonic.lw) && !(currInstruction.mnem == Mnemonic.sw) && !ROB.isFull() && !RSfullFlag) {
                // places the instruction in the RS
                for(int i = 0; i < RSsize; i++) {
                    if(!reservationStation[i].busy) {
                        reservationStation[i].ALUop        = currInstruction.aluop;
                        reservationStation[i].ROBEN        = tempROBen + 1;
                        reservationStation[i].busy         = true;

                        // assigns operands based on format
                        if(currInstruction.format == "R") {
                            if(currInstruction.mnem == Mnemonic.sll || currInstruction.mnem == Mnemonic.srl){
                                // assigns the ROBEN of the first operand
                                reservationStation[i].ROBEN1 = regs_ROBENS[currInstruction.rtind];
                                // search the ROB for the value needed
                                if(reservationStation[i].ROBEN1 == 0)
                                    reservationStation[i].ROBEN1_val = regs[currInstruction.rtind];
                                else if(!ROB.isEmpty()){
                                    int j = ROB.start;
                                    ROBregister currROBregister = new ROBregister(currInstruction.mnem);
                                    do{
                                        currROBregister = ROB.getIndex(j);
                                        if(currROBregister.busy && currROBregister.ready && ((j + 1) == reservationStation[i].ROBEN1)) {
                                            reservationStation[i].ROBEN1_val = currROBregister.writeData;
                                            reservationStation[i].ROBEN1 = 0;
                                        }   
                                        j = (j+1) % ROB.size;
                                    } while (j != (ROB.end + 1) % ROB.size);
                                }
                            }
                            else {
                                // assigns the ROBEN of the first operand
                                reservationStation[i].ROBEN1 = regs_ROBENS[currInstruction.rsind];
                                // search the ROB for the value needed
                                if(reservationStation[i].ROBEN1 == 0)
                                    reservationStation[i].ROBEN1_val = regs[currInstruction.rsind];
                                else if(!ROB.isEmpty()){
                                    int j = ROB.start;
                                    ROBregister currROBregister = new ROBregister(currInstruction.mnem);
                                    do{
                                        currROBregister = ROB.getIndex(j);
                                        if(currROBregister.busy && currROBregister.ready && ((j + 1) == reservationStation[i].ROBEN1)) {
                                            reservationStation[i].ROBEN1_val = currROBregister.writeData;
                                            reservationStation[i].ROBEN1 = 0;
                                        }   
                                        j = (j+1) % ROB.size;
                                    } while (j != (ROB.end + 1) % ROB.size);
                                }
                            }

                            if(currInstruction.mnem == Mnemonic.sll || currInstruction.mnem == Mnemonic.srl){
                                reservationStation[i].ROBEN2 = 0;
                                reservationStation[i].ROBEN2_val = currInstruction.oper2;
                            }
                            else if(currInstruction.mnem == Mnemonic.jr) {
                                reservationStation[i].ROBEN2 = 0;
                                reservationStation[i].ROBEN2_val = 0;
                            }
                            else {
                                // assigns the ROBEN of the second operand
                                reservationStation[i].ROBEN2 = regs_ROBENS[currInstruction.rtind];
                                // search the ROB for the value needed
                                if(reservationStation[i].ROBEN2 == 0)
                                    reservationStation[i].ROBEN2_val = regs[currInstruction.rtind];
                                else if(!ROB.isEmpty()){
                                    int j = ROB.start;
                                    ROBregister currROBregister = new ROBregister(currInstruction.mnem);
                                    do {
                                        currROBregister = ROB.getIndex(j);
                                        if(currROBregister.busy && currROBregister.ready && ((j + 1) == reservationStation[i].ROBEN2)) {
                                            reservationStation[i].ROBEN2_val = currROBregister.writeData;
                                            reservationStation[i].ROBEN2 = 0;
                                        }
                                        j = (j+1) % ROB.size;
                                    } while (j != (ROB.end + 1) % ROB.size) ;
                                }
                            }
                        }
                        else if(currInstruction.format == "I") {
                            // assigns the ROBEN of the first operand
                            reservationStation[i].ROBEN1 = regs_ROBENS[currInstruction.rsind];
                            // search the ROB for the value needed
                            if(reservationStation[i].ROBEN1 == 0)
                                reservationStation[i].ROBEN1_val = regs[currInstruction.rsind];
                            else if(!ROB.isEmpty()){
                                int j = ROB.start;
                                ROBregister currROBregister = new ROBregister(currInstruction.mnem);
                                do{
                                    currROBregister = ROB.getIndex(j);
                                    if(currROBregister.busy && currROBregister.ready && ((j + 1) == reservationStation[i].ROBEN1)) {
                                        reservationStation[i].ROBEN1_val = currROBregister.writeData;
                                        reservationStation[i].ROBEN1 = 0;
                                    }   
                                    j = (j+1) % ROB.size;
                                } while (j != (ROB.end + 1) % ROB.size);
                            }

                            if(currInstruction.mnem == Mnemonic.bne || currInstruction.mnem == Mnemonic.beq) {
                                // assigns the ROBEN of the second operand
                                reservationStation[i].ROBEN2 = regs_ROBENS[currInstruction.rtind];
                                // search the ROB for the value needed
                                if(reservationStation[i].ROBEN2 == 0)
                                    reservationStation[i].ROBEN2_val = regs[currInstruction.rtind];
                                else if(!ROB.isEmpty()){
                                    int j = ROB.start;
                                    ROBregister currROBregister = new ROBregister(currInstruction.mnem);
                                    do{
                                        currROBregister = ROB.getIndex(j);
                                        if(currROBregister.busy && currROBregister.ready && ((j + 1) == reservationStation[i].ROBEN2)) {
                                            reservationStation[i].ROBEN2_val = currROBregister.writeData;
                                            reservationStation[i].ROBEN2 = 0;
                                        }   
                                        j = (j+1) % ROB.size;
                                    } while (j != (ROB.end + 1) % ROB.size);
                                }
                            }
                            else {
                                reservationStation[i].ROBEN2 = 0;
                                reservationStation[i].ROBEN2_val = currInstruction.oper2;
                            }   
                        }
                        
                        break;
                    }
                }

                // updates register file ROBEN
                if(currInstruction.rdind != 0 && currInstruction.mnem != Mnemonic.beq && currInstruction.mnem != Mnemonic.bne
                && currInstruction.mnem != Mnemonic.jr) 
                    regs_ROBENS[currInstruction.rdind] = tempROBen + 1;

                

                return true;
            }
            else if((currInstruction.mnem == Mnemonic.lw || currInstruction.mnem == Mnemonic.sw) && !ROB.isFull() && !LSbuffer.isFull()) {
                // places the instruction in the LSbuffer
                LSregister currLSregister = new LSregister(currInstruction.mnem);
                currLSregister.ready = false;
                currLSregister.busy = true;
                currLSregister.Rd = currInstruction.rdind;
                currLSregister.ROBEN = tempROBen + 1;
                currLSregister.immediate = currInstruction.immeds;

                // assigns the ROBEN of the first operand
                currLSregister.ROBEN1 = regs_ROBENS[currInstruction.rsind];

                // search the ROB for the value needed
                if(currLSregister.ROBEN1 == 0) {
                    currLSregister.ROBEN1_val = regs[currInstruction.rsind];
                    currLSregister.effectiveAddress = currLSregister.ROBEN1_val + currLSregister.immediate;
                }    
                else if(!ROB.isEmpty()){
                    int j = ROB.start;
                    ROBregister currROBregister = new ROBregister(currInstruction.mnem);
                    do{
                        currROBregister = ROB.getIndex(j);
                        if(currROBregister.busy && currROBregister.ready && ((j + 1) == currLSregister.ROBEN1)) {
                            currLSregister.ROBEN1_val = currROBregister.writeData;
                            currLSregister.ROBEN1 = 0;
                            currLSregister.effectiveAddress = currLSregister.ROBEN1_val + currLSregister.immediate;
                        }     
                        j = (j+1) % ROB.size;
                    } while (j != (ROB.end + 1) % ROB.size);
                }
                // assigns the ROBEN of the second operand
                if(currInstruction.mnem == Mnemonic.lw) {
                    currLSregister.ROBEN2 = 0;
                    currLSregister.ROBEN2_val = currInstruction.oper2;
                }
                else {
                    currLSregister.ROBEN2 = regs_ROBENS[currInstruction.rtind];
                    // search the ROB for the value needed
                    if(currLSregister.ROBEN2 == 0) 
                        currLSregister.ROBEN2_val = regs[currInstruction.rtind];
                    else if(!ROB.isEmpty()){
                        int j = ROB.start;
                        ROBregister currROBregister = new ROBregister(currInstruction.mnem);
                        do{
                            currROBregister = ROB.getIndex(j);
                            if(currROBregister.busy && currROBregister.ready && ((j + 1) == currLSregister.ROBEN2)) {
                                currLSregister.ROBEN2_val = currROBregister.writeData;
                                currLSregister.ROBEN2 = 0;
                            }    
                            j = (j+1) % ROB.size;
                        } while (j != (ROB.end + 1) % ROB.size);
                    }
                }
                // updates the ready bit if necessary
                currLSregister.ready = currLSregister.ROBEN1 == 0 && currLSregister.ROBEN2 == 0;
            
                // places the register in the LSbuffer
                LSbuffer.enqueue(currLSregister);

                // updates register file ROBEN
                if(currInstruction.mnem == Mnemonic.lw && currInstruction.rdind !=0) { regs_ROBENS[currInstruction.rdind] = tempROBen + 1; }

                return true;
            }
            else { return false; } // buffers or ROB is/are full
        }

        // dispatch instruction from instruction queue
        public int dispatch_ROB(Instruction currInstruction, bool prediction) {
            int tempROBen = 0;
            
            ROBregister currROBregister = new ROBregister(currInstruction.mnem);
            currROBregister.Rd          = currInstruction.rdind;
            currROBregister.busy        = true;
            currROBregister.ready       = false;
            currROBregister.exception   = false;
            currROBregister.PC          = currInstruction.PC;

            if(currInstruction.mnem == Mnemonic.beq || currInstruction.mnem == Mnemonic.bne) {
                    // assigns the write data as the branch address
                    currROBregister.writeData = currInstruction.immeds + currInstruction.PC;
                    // assigns the prediction (taken or not taken 0 = NT & 1 = T)
                    currROBregister.branchPrediction = prediction;
                    // if the branch is predicted as taken, the next instruction fetched should be at the branch address
                    if(prediction) {
                        targetaddress = currInstruction.immeds + currInstruction.PC;
                        pcsrc = PCsrc.branchTarget;
                    }
                } 
                
            if(currInstruction.mnem == Mnemonic.j || currInstruction.mnem == Mnemonic.jal) {
                currROBregister.writeData = currInstruction.address;
                currROBregister.ready       = true;
                tempROBen = ROB.enqueue(currROBregister);
                if(currInstruction.mnem == Mnemonic.jal) regs_ROBENS[31] = ROB.end;
                return tempROBen;
            }
            
            tempROBen = ROB.enqueue(currROBregister);
            
            return tempROBen;
        }

        public (int, int) execute(Aluop ALUop, int operand1, int operand2, int ROBEN) {
            switch (ALUop) {
                case Aluop.add: return (operand1 + operand2, ROBEN);
                case Aluop.mult: return (operand1 * operand2, ROBEN);
                case Aluop.sub: return (operand1 - operand2, ROBEN);
                case Aluop.and: return (operand1 & operand2, ROBEN);
                case Aluop.or : return (operand1 | operand2, ROBEN);
                case Aluop.xor: return (operand1 ^ operand2, ROBEN);
                case Aluop.nor: return (~(operand1 | operand2), ROBEN);
                case Aluop.slt: return (((operand1 < operand2) ? 1 : 0), ROBEN);
                case Aluop.seq: return (((operand1 == operand2) ? 1 : 0), ROBEN);
                case Aluop.sne: return (((operand1 != operand2) ? 1 : 0), ROBEN);
                case Aluop.sgt: return (((operand1 > operand2) ? 1 : 0), ROBEN);
                case Aluop.sll: return (operand1 << operand2, ROBEN);
                case Aluop.srl: {
                        if (operand2 == 0)
                            return (operand1, ROBEN);
                        return ((operand1 >>> operand2), ROBEN);
                    };
                default: return (0, 0);
            }
        }

        public (int, int) executeHead(){
            int result = 0;
            int resultROBEN = 0;
            // executes ready instruction from RS
            for(int i = RSsize - 1; i >= 0; i--) {
                if(reservationStation[i].busy && reservationStation[i].ROBEN1 == 0 && reservationStation[i].ROBEN2 == 0) {
                    (result, resultROBEN) = execute(reservationStation[i].ALUop, reservationStation[i].ROBEN1_val, reservationStation[i].ROBEN2_val, reservationStation[i].ROBEN);
                    reservationStation[i].busy = false; // frees the register after execution
                    break;
                }
            }

            // executes ready instruction from LS buffer 
            if(!LSbuffer.isEmpty()) {
                LSregister currLSregister = LSbuffer.getIndex(LSbuffer.start);
                if(currLSregister.busy && currLSregister.ready) {
                    if(currLSregister.type == Mnemonic.lw) {
                        LSbuffer.dequeue();
                        result = Convert.ToInt32(DM[currLSregister.effectiveAddress]);
                        resultROBEN = currLSregister.ROBEN;
                        currLSregister.busy = false;
                    }
                    else if(currLSregister.type == Mnemonic.sw) {
                        if((ROB.start + 1)== currLSregister.ROBEN) {
                            ROB.getIndex(ROB.start).ready = true;
                            LSbuffer.dequeue();
                            DM[currLSregister.effectiveAddress] = Convert.ToString(currLSregister.ROBEN2_val);
                        }
                    }
                }
            }

            return (result, resultROBEN);
        }

        public void CDB_RS_LS(int result, int resultROBEN) {
            // updates RS operands from CDB
            for(int i = 0; i < RSsize; i++){
                if(reservationStation[i].busy) {
                    // updates first operand
                    if(reservationStation[i].ROBEN1 == resultROBEN && resultROBEN != 0) { 
                        reservationStation[i].ROBEN1_val = result; 
                        reservationStation[i].ROBEN1 = 0;
                    }
                    // updates second operand
                    if(reservationStation[i].ROBEN2 == resultROBEN && resultROBEN != 0) { 
                        reservationStation[i].ROBEN2_val = result;
                        reservationStation[i].ROBEN2 = 0;
                    }
                }
            }

            // updates load store buffer
            int k = LSbuffer.start;
            if(!LSbuffer.isEmpty()) {
                do{
                    LSregister currLSregister = LSbuffer.getIndex(k);
                    if(currLSregister.busy) {
                        // updates first operand
                        if(currLSregister.ROBEN1 == resultROBEN && resultROBEN != 0) {
                            currLSregister.ROBEN1_val = result;
                            currLSregister.ROBEN1 = 0;
                            currLSregister.effectiveAddress = currLSregister.ROBEN1_val + currLSregister.immediate;
                        }
                        // updates second operand for store instructions
                        if(currLSregister.type == Mnemonic.sw && currLSregister.ROBEN2 == resultROBEN && resultROBEN != 0) {
                            currLSregister.ROBEN2_val = result;
                            currLSregister.ROBEN2 = 0;
                        }
                        // updates the ready bit if necessary
                        currLSregister.ready = currLSregister.ROBEN1 == 0 && currLSregister.ROBEN2 == 0; 
                    }
                    k = (k + 1) % LSbuffer.size;
                } while (k != (LSbuffer.end + 1) % LSbuffer.size);
            }
        } 

        public void CDB_ROB(int result, int resultROBEN) {
            // updates ROB from CDB
            int k = ROB.start;
            if(!ROB.isEmpty()) {
                do{ 
                    ROBregister currROBregister = ROB.getIndex(k);
                    if(k + 1 == resultROBEN) { 
                        // ensures we do not overite the branch address for branch instructions
                        if(!(currROBregister.type == Mnemonic.bne || currROBregister.type == Mnemonic.beq)) currROBregister.writeData   = result;
                        currROBregister.ready       = true;
                        // branch decision = 1 (taken) = 0 (not taken)
                        currROBregister.branchDecision = (currROBregister.type == Mnemonic.beq && result == 0) || (currROBregister.type == Mnemonic.bne && result != 0);
                    }
                    k = (k + 1) % ROB.size;
                } while (k != (ROB.end + 1) % ROB.size);
            }
        }   

        public bool BranchPredictor(bool wrongPrediction, Mnemonic commitOpcode) {
            // updates the state of the pranch predictor
            if(commitOpcode == Mnemonic.bne || commitOpcode == Mnemonic.beq) {
                if(wrongPrediction && predictorState == 0)          predictorState = 1;
                else if(wrongPrediction && predictorState == 1)     predictorState = 2;
                else if(wrongPrediction && predictorState == 2)     predictorState = 1;
                else if(wrongPrediction && predictorState == 3)     predictorState = 2;
                else if(!wrongPrediction && predictorState == 0)    predictorState = 0;
                else if(!wrongPrediction && predictorState == 1)    predictorState = 0;
                else if(!wrongPrediction && predictorState == 2)    predictorState = 3;
                else if(!wrongPrediction && predictorState == 3)    predictorState = 3;
                else throw new Exception($"Prediction combination error");
            }

            // 0 = NT & 1 = T
            if (predictorState == 0) return false;
            else if(predictorState == 1) return false;
            else if(predictorState == 2) return true;
            else if(predictorState == 3) return true;
            else throw new Exception($"Prediction state error");
        }

        public void flushRegisters() {
            for(int i = 0; i < 16; i++){
                reservationStation[i].busy = false;
                reservationStation[i].ROBEN = 0;
                reservationStation[i].ROBEN1 = 0;
                reservationStation[i].ROBEN2 = 0;
            }

            for(int i = 0; i < 32; i++) regs_ROBENS[i] = 0;

            if(!ROB.isEmpty()) {
                int j = ROB.start;
                ROBregister currROBregister = new ROBregister(Mnemonic.add);
                do{
                    currROBregister = ROB.getIndex(j);
                    currROBregister.clear();
                    j = (j+1) % ROB.size;
                } while (j != (ROB.end + 1) % ROB.size);
                ROB.flush();
            }
            
            if(!LSbuffer.isEmpty()) {
                int j = LSbuffer.start;
                LSregister currLSregister = new LSregister(Mnemonic.add);
                do{
                    currLSregister = LSbuffer.getIndex(j);
                    currLSregister.clear();
                    j = (j+1) % LSbuffer.size;
                } while (j != (LSbuffer.end + 1) % LSbuffer.size);
                LSbuffer.flush();
            }
            flush = false;
        }

        public (Mnemonic, bool, bool, int) commit() {
            int commitBTA = 0;
            // if empty, return
            if(ROB.isEmpty()) return (Mnemonic.nop, false, false, commitBTA);

            ROBregister currROBregister = ROB.getIndex(ROB.start);
            if(currROBregister.type == Mnemonic.hlt) {
                hlt = true;
                currROBregister.clear();
                ROB.dequeue();
                return (Mnemonic.hlt, false, false, commitBTA);
            }

            if(currROBregister.type == Mnemonic.sll && currROBregister.Rd == 0){
                currROBregister.clear();
                ROB.dequeue();
                return (Mnemonic.nop, false, false, commitBTA);
            }

            if(currROBregister.ready) {
                // if branch instruction, branch
                if(currROBregister.type == Mnemonic.beq || currROBregister.type == Mnemonic.bne) {
                    // check if the prediction was correct (decision and prediction are equal)
                    if(currROBregister.branchPrediction == currROBregister.branchDecision) {
                        currROBregister.clear();
                        ROB.dequeue();
                        return (currROBregister.type, false, false, commitBTA);
                    }
                    else // wrong prediction
                    {
                        if(currROBregister.branchDecision) 
                            commitBTA = currROBregister.writeData;
                        else
                            commitBTA = 1 + currROBregister.PC;

                        flushRegisters();

                        return (currROBregister.type, true, true, commitBTA);
                    }
                }
                else if(currROBregister.type == Mnemonic.j || currROBregister.type == Mnemonic.jal || currROBregister.type == Mnemonic.jr) {
                    if(currROBregister.type == Mnemonic.jal) {
                        regs_ROBENS[31] = 0;
                        regs[31] = currROBregister.PC + 1;
                    }
                    ROB.dequeue();
                    return (currROBregister.type, false, false, commitBTA);
                }
                else if(currROBregister.type == Mnemonic.sw){
                    currROBregister.clear();
                    ROB.dequeue();
                    return (currROBregister.type, false, false, commitBTA);
                } 
                else {
                    if(currROBregister.Rd != 0 ) {
                        if(regs_ROBENS[currROBregister.Rd] == (ROB.start + 1 % 16))
                            regs_ROBENS[currROBregister.Rd] = 0;
                            
                        regs[currROBregister.Rd] = currROBregister.writeData;
                    }
                } 

                currROBregister.clear();
                ROB.dequeue();
            }
            return (currROBregister.type, false, false, commitBTA);
        }

        public void update_PC(Instruction currInstruction, bool flushFlag, bool wrongPrediction, int commitBTA) {
            if(currInstruction.mnem == Mnemonic.j || currInstruction.mnem == Mnemonic.jal) { 
                targetaddress = currInstruction.address;
                pcsrc = PCsrc.branchTarget;
            }
            else if(currInstruction.mnem == Mnemonic.jr) {
                int pcRegIndex = currInstruction.rsind;
                // register is ready
                if(regs_ROBENS[pcRegIndex] == 0) {
                    targetaddress = regs[pcRegIndex];
                    pcsrc = PCsrc.branchTarget;
                }
                else    
                    pcsrc = PCsrc.none;
            }
            else if(currInstruction.mnem == Mnemonic.hlt)
                pcsrc = PCsrc.none;

            // from the previous cycle
            if(flushFlag) {
                pcsrc = PCsrc.branchTarget;
                targetaddress = commitBTA;
            }

            if (pcsrc == PCsrc.none) return; 
            else if (pcsrc == PCsrc.PCplus1) PC += 1; 
            else if (pcsrc == PCsrc.branchTarget) {
                PC = targetaddress; 
                pcsrc = PCsrc.PCplus1;
            }

        }

        (bool, bool, int, int, int) positiveEdge(    Instruction currInstruction, 
                                                bool prediction, 
                                                bool wrongPrediction, 
                                                int result, 
                                                int resultROBEN,
                                                bool flushFlag,
                                                bool prevFlushFlag,
                                                int prevCommitBTA,
                                                Mnemonic commitOpcode = Mnemonic.nop
                                                ) {
            int tempROBEN = 0;
            CDB_ROB(result, resultROBEN);
            if(currInstruction.mnem == Mnemonic.bne || currInstruction.mnem == Mnemonic.beq) 
                prediction = BranchPredictor(wrongPrediction, Mnemonic.nop);
            BranchPredictor(wrongPrediction, commitOpcode);
            
            update_PC(currInstruction, flushFlag, wrongPrediction, prevCommitBTA); // updates the program counter

            if(!flushFlag)  tempROBEN = dispatch_ROB(currInstruction, prediction);

            return (prediction, wrongPrediction, result, resultROBEN, tempROBEN);
        }

        (Instruction, Mnemonic, int, int, bool, int) negativeEdge(   Instruction currInstruction, 
                                                bool prediction, 
                                                bool wrongPrediction,
                                                int result,
                                                int resultROBEN, 
                                                int tempROBEN,
                                                int cycles,
                                                bool flushFlag,
                                                int commitBTA
                                                ){

            CDB_RS_LS(result, resultROBEN);                                          
            (result, resultROBEN) = executeHead(); // executes ready instructions                                           
            if(cycles > 1 && !flushFlag) dispatch_RS_LS(currInstruction, prediction, tempROBEN); // dispatches the instruction into the ROB & LS or RS
            string mc = IM[PC]; // fetch the instructon
            currInstruction = decodemc(mc, PC); // decode the instruction
            (Mnemonic commitOpcode, wrongPrediction, flushFlag, commitBTA) = commit(); // commits instruction from the ROB
            if (flushFlag) resultROBEN = 0;
            return (currInstruction, commitOpcode, result, resultROBEN, flushFlag, commitBTA);
        }


        public (int, Exceptions) Run() {
            int cycles = 0;
            Exceptions excep = Exceptions.NONE;
            // branch predictor initializations
            bool prediction = false; 
            bool wrongPrediction = false;
            int result = 0;
            int resultROBEN = 0;
            int tempROBEN = 0;
            bool flushFlag = false;
            bool prevFlushFlag = false;
            int commitBTA = 0;
            int prevCommitBTA = 0;

            Instruction currInstruction = GetNewInst();
            Mnemonic commitOpcode = Mnemonic.nop;

            while (PC < IM.Count) {
                cycles++;
                try {
                    (currInstruction, 
                     commitOpcode,
                     result,
                     resultROBEN,
                     flushFlag,
                     commitBTA) = negativeEdge(   currInstruction, 
                                                    prediction, 
                                                    wrongPrediction, 
                                                    result, 
                                                    resultROBEN,
                                                    tempROBEN,
                                                    cycles,
                                                    flushFlag,
                                                    commitBTA
                                                    );
                    (prediction, 
                     wrongPrediction, 
                     result, 
                     resultROBEN, 
                     tempROBEN) = positiveEdge( currInstruction, 
                                                prediction, 
                                                wrongPrediction, 
                                                result, 
                                                resultROBEN, 
                                                flushFlag,
                                                prevFlushFlag,
                                                commitBTA,
                                                commitOpcode
                                                );
                    prevFlushFlag = flushFlag && (commitOpcode == Mnemonic.bne || commitOpcode == Mnemonic.beq);
                    prevCommitBTA = commitBTA;
                    
                }
                catch (Exception e) {
                    if (e.Message == EXCEPTION) {
                        Run();
                        return (cycles, excep);
                    }
                }
                if(hlt) return (cycles, excep);
                if (cycles == RISCV.MAX_CLOCKS)
                {
                    excep = Exceptions.INF_LOOP;
                    return (cycles, excep);
                }
                
            }

            return (cycles, excep);
        }

        public void print_regs() { RISCV.print_regs(regs); }
        public void print_DM() { RISCV.print_DM(DM); }

    }

    
    public struct BranchPredictorEntry
    {
        public int PC;
        public string BranchHistory;
        public bool outcome;
    }

    public class CPU6STAGE
    {
        int PC;
        int state = 0;
        bool hlt;
        bool WrongPrediction;
        int targetaddress;
        public List<string> DM; // Data Mem
        public List<int> regs;
        public List<string> IM; // Instruction Mem
        public List<BranchPredictorEntry> dataset = [];
        public string BranchHistory = "0000000000000000";

        Instruction IFID;
        Instruction IDEX1;
        Instruction IDEX2;
        Instruction EXMEM;
        Instruction MEMWB;

        Instruction memed_in_MEM_MIPS;
        Instruction executed1_in_EX_MIPS;
        Instruction executed2_in_EX_MIPS;
        Instruction decoded_in_ID_MIPS;
        string fetched_in_IF_MIPS;

        int ID_HAZ;
        int EX_HAZ;
        int MEM_HAZ;
        enum PCsrc
        {
            PCplus1, pfc, exception, none
        }
        PCsrc pcsrc;
        public CPU6STAGE(List<string> insts, List<string> data_mem_init)
        {
            (IM, DM, regs) = InitMipsCPU(insts, data_mem_init);

            PC = -1;
            hlt = false;
            WrongPrediction = false;
            targetaddress = 0;

            IFID = GetNewInst();
            IDEX1 = GetNewInst();
            IDEX2 = GetNewInst();
            EXMEM = GetNewInst();
            MEMWB = GetNewInst();
            memed_in_MEM_MIPS = GetNewInst();
            executed1_in_EX_MIPS = GetNewInst();
            executed2_in_EX_MIPS = GetNewInst();
            decoded_in_ID_MIPS = GetNewInst();
            fetched_in_IF_MIPS = "";
            EX_HAZ = 0;
            MEM_HAZ = 0;
        }
        int forward(int source_ind, int source_reg)
        {
            // ID_HAZ
            if (IDEX2.rdind != 0 && iswb(IDEX2.mnem) && IDEX2.rdind == source_ind)
            {
                return ID_HAZ;
            }
            // EX haz
            else if (EXMEM.rdind != 0 && iswb(EXMEM.mnem) && EXMEM.rdind == source_ind)
            {
                return EX_HAZ;
            }
            // MEM haz
            else if (MEMWB.rdind != 0 && iswb(MEMWB.mnem) && MEMWB.rdind == source_ind)
            {
                return MEM_HAZ;
            }
            return source_reg;
        }
        void update_PC() {
            if (pcsrc == PCsrc.none) return;
            else if (pcsrc == PCsrc.PCplus1) PC += 1; 
            else if (pcsrc == PCsrc.pfc)
            {
                PC = targetaddress;
            }
        }
        Instruction decodemc(string mc, int pc)
        {
            Instruction inst = GetNewInst();
            inst.mc = mc;
            inst.PC = pc;
            inst.opcode = mc.Substring(mc.Length - (1 + 31), 6);
            inst.rsind = Convert.ToInt32(mc.Substring(mc.Length - (1 + 25), 5), 2);
            inst.rtind = Convert.ToInt32(mc.Substring(mc.Length - (1 + 20), 5), 2);
            inst.rdind = Convert.ToInt32(mc.Substring(mc.Length - (1 + 15), 5), 2);
            inst.shamt = mc.Substring(mc.Length - (1 + 10), 5);
            inst.funct = mc.Substring(mc.Length - (1 + 5), 6);
            inst.immeds = Convert.ToInt32(sx(mc.Substring(mc.Length - (1 + 15), 16)), 2);
            inst.immedz = Convert.ToInt32(zx(mc.Substring(mc.Length - (1 + 15), 16)), 2);
            inst.address = Convert.ToInt32(zx(mc.Substring(mc.Length - (1 + 25), 26)), 2);

            // riscv integer instruction map for formats (R, I, J)
            inst.rs = regs[inst.rsind];
            inst.rt = regs[inst.rtind];


            if (inst.opcode != "000000")
                inst.funct = "000000";
            if (!mnemonicmap.TryGetValue(inst.opcode + inst.funct, out Mnemonic value))
            {
                Exception e = new Exception(EXCEPTION)
                {
                    Source = DECODE
                };
                throw e;
            }
            else
            {
                if (mc == "".PadLeft(32, '0'))
                    inst.mnem = Mnemonic.nop;
                inst.mnem = value;
            }
            inst.aluop = get_inst_aluop(inst.mnem);
            inst.format = get_format(inst.mnem);
            inst.oper1 = get_oper1(inst);
            inst.oper2 = get_oper2(inst);
            if (inst.format == "I")
            {
                inst.rdind = inst.rtind;
            }
            if (inst.format == "J")
            {
                inst.rdind = 31;
            }
            return inst;
        }

        bool BranchPredictor(Instruction decoded)
        {
            bool prediction;
            if (!isbranch(decoded.mnem))
            {
                prediction = false;
            }
            else
            {
                int index = decoded.PC;
                prediction = (state == 2 || state == 3);
            }

            if (prediction)
            {
                pcsrc = PCsrc.pfc;
            }
            else
            {
                pcsrc = PCsrc.PCplus1;
            }

            if (isbranch(executed2_in_EX_MIPS.mnem))
            {
                if (state <= 1)
                {
                    if (WrongPrediction)
                    {
                        state++;
                    }
                    else
                    {
                        if (state == 1) state = 0;
                    }
                }
                else
                {
                    if (WrongPrediction)
                    {
                        state--;
                    }
                    else
                    {
                        if (state == 2) state = 3;
                    }
                }
            }

            return prediction;
        }

        void BranchResolver(Instruction decoded)
        {
            if (WrongPrediction)
            {
                WrongPrediction = false;
                throw new Exception(BUBBLE) { Source = WRONG_PRED };
            }
            else if (executed2_in_EX_MIPS.mnem == Mnemonic.lw && executed2_in_EX_MIPS.rdind != 0)
            {
                int rdind2 = executed2_in_EX_MIPS.rdind;
                if (decoded.rsind == rdind2 || decoded.rtind == rdind2)
                {
                    throw new Exception(BUBBLE) { Source = LOAD_USE };
                }
            }
            else if (executed1_in_EX_MIPS.mnem == Mnemonic.lw && executed1_in_EX_MIPS.rdind != 0)
            {
                int rdind1 = executed1_in_EX_MIPS.rdind;
                if (decoded.rsind == rdind1 || decoded.rtind == rdind1)
                {
                    throw new Exception(BUBBLE) { Source = LOAD_USE };
                }
            }
            else if (decoded.mnem == Mnemonic.jr)
            {
                throw new Exception(BUBBLE) { Source = JR_INDECODE };
            }
            if (decoded.mnem == Mnemonic.hlt)
            {
                pcsrc = PCsrc.none;
            }
            else // here we handle the branch prediction
            {
                if (executed1_in_EX_MIPS.mnem == Mnemonic.jr)
                {
                    pcsrc = PCsrc.pfc;
                    targetaddress = executed1_in_EX_MIPS.oper1;
                }
                else if (decoded.mnem == Mnemonic.beq || decoded.mnem == Mnemonic.bne)
                {
                    if (decoded.prediction)
                        targetaddress = decoded.PC + decoded.immeds;
                }
                else if (decoded.mnem == Mnemonic.j || decoded.mnem == Mnemonic.jal)
                {
                    pcsrc = PCsrc.pfc;
                    targetaddress = decoded.address;
                }
                else
                    pcsrc = PCsrc.PCplus1;
            }
        }
        string fetch()
        {
            string fetched = "";
            if (PC >= IM.Count) return fetched.PadLeft(32, '0');
            fetched = IM[PC];
            return fetched;
        }
        Instruction decode(string fetched)
        {
            Instruction decoded = GetNewInst();
            try
            {
                decoded = decodemc(fetched, PC);
                decoded.prediction = BranchPredictor(decoded);
                BranchResolver(decoded);
            }
            catch (Exception e)
            {
                decoded_in_ID_MIPS = decoded;
                throw e;
            }
            update_PC();
            detect_exception(decoded, Stage.decode);
            return decoded;
        }
        public List<string> GetDataSet()
        {
            List<string> ret = [];
            foreach(BranchPredictorEntry entry in dataset)
            {
                ret.Add($"{entry.PC.ToString("X").PadLeft(8, '0'),-8}, \"{entry.BranchHistory}\", {(entry.outcome ? "1" : "0")}");
            }
            return ret;
        }
        Instruction execute2(Instruction forwarded)
        {
            Instruction temp = forwarded;
            if (forwarded.mnem == Mnemonic.nop)
            {
                return temp;
            }
            detect_exception(forwarded, Stage.execute2);
            temp.aluout = execute_inst(temp);
            if (temp.mnem == Mnemonic.jal)
                temp.aluout = temp.PC + 1;
            bool BranchDecision = temp.mnem == Mnemonic.beq && temp.oper1 == temp.oper2 ||
                                  temp.mnem == Mnemonic.bne && temp.oper1 != temp.oper2;
            WrongPrediction = temp.prediction != BranchDecision;

            if (dataset.Count < 10000 && isbranch(temp.mnem))
            {
                dataset.Add(new() { PC = temp.PC, BranchHistory = BranchHistory, outcome = BranchDecision });
                BranchHistory = (BranchDecision ? "1" : "0") + BranchHistory;
                if (BranchHistory.Length > 16)
                    BranchHistory = BranchHistory.Substring(0, 16);
            }
            ID_HAZ = temp.aluout;
            return temp;
        }
        Instruction execute1(Instruction decoded)
        {
            Instruction temp = decoded;
            if (decoded.mnem == Mnemonic.nop)
            {
                return temp;
            }
            if (temp.format == "R")
            {
                if (temp.mnem == Mnemonic.sll || temp.mnem == Mnemonic.srl)
                {
                    temp.oper1 = forward(temp.rtind, temp.oper1);
                }
                else
                {
                    temp.oper1 = forward(temp.rsind, temp.oper1);
                    temp.oper2 = forward(temp.rtind, temp.oper2);
                }
            }
            else if (temp.format == "I")
            {
                temp.oper1 = forward(temp.rsind, temp.oper1);
                if (isbranch(temp.mnem))
                    temp.oper2 = forward(temp.rtind, temp.oper2);
                // we will update the second operand if the instruction uses them 
                // (i.e. when it's an I-format and sw)
                if (temp.mnem == Mnemonic.sw)
                {
                    temp.rt = forward(temp.rtind, temp.rt);
                }
            }
            detect_exception(decoded, Stage.execute1);
            return temp;
        }
        Instruction mem(ref Instruction inst)
        {
            Instruction temp = inst;
            if (inst.mnem == Mnemonic.nop)
            {
                EX_HAZ = 0;
                return temp;
            }
            detect_exception(inst, Stage.memory);
            if (temp.mnem == Mnemonic.lw)
            {
                temp.memout = Convert.ToInt32(DM[temp.aluout]);
            }
            else if (temp.mnem == Mnemonic.sw)
            {
                DM[temp.aluout] = Convert.ToString(temp.rt);
            }

            EX_HAZ = temp.aluout;

            return temp;
        }
        void write_back(Instruction inst)
        {
            if (inst.mnem == Mnemonic.nop)
            {
                MEM_HAZ = 0;
                return;
            }
            if (inst.mnem == Mnemonic.hlt)
                hlt = true;
            if (!iswb(inst.mnem) || inst.rdind == 0)
                return;
            detect_exception(inst, Stage.write_back);
            regs[inst.rdind] = (inst.mnem == Mnemonic.lw) ? inst.memout : inst.aluout;
            MEM_HAZ = (inst.mnem == Mnemonic.lw) ? inst.memout : inst.aluout;
        }
        void handle_exception(Exception e)
        {
            PC = HANDLER_ADDR;
            IFID.mc = fetch();

            string s = "NO MESSAGE (handle_exception)";
            if (e.Source != null)
                s = e.Source;

            if (s == DECODE)
            {
                IDEX1 = GetNewInst();
                IDEX2 = GetNewInst();
                EXMEM = executed2_in_EX_MIPS;
                MEMWB = memed_in_MEM_MIPS;
            }
            else if (s == EXECUTE)
            {
                IDEX1 = GetNewInst();
                IDEX2 = GetNewInst();
                EXMEM = GetNewInst();
                MEMWB = memed_in_MEM_MIPS;
            }
            else if (s == MEMORY || s == WRITEBACK) // made them in a single if condition because they have the same effect in an exception case
            {
                IDEX1 = GetNewInst();
                IDEX2 = GetNewInst();
                EXMEM = GetNewInst();
                MEMWB = GetNewInst();
                EX_HAZ = 0;
                MEM_HAZ = (s == WRITEBACK) ? 0 : MEM_HAZ;
            }

        }
        void detect_exception(Instruction inst, Stage stage)
        {
            Exception e = new Exception(EXCEPTION);

            if (stage == Stage.decode)
            {
                // detect if there is an exception in the decode operation in the decode stage
                if ((!isvalid_opcode_funct(inst) || !isvalid_format(inst.format)) && inst.mnem != Mnemonic.nop)
                {
                    e.Source = DECODE;
                    throw e;
                }
                if (isbranch(inst.mnem) || inst.mnem == Mnemonic.j || inst.mnem == Mnemonic.jal || inst.mnem == Mnemonic.jr)
                {
                    if (!is_in_range_inc(PC, 0, IM.Count - 1) && !(PC == IM.Count))
                    {
                        e.Source = DECODE;
                        throw e;
                    }
                }
            }
            else if (stage == Stage.execute1)
            {
            }
            else if (stage == Stage.execute2)
            {
                // detect if there is an exception in the execution of the instruction in the execute stage (/0)
                if (inst.oper2 == 0 && inst.aluop == Aluop.div)
                {
                    e.Source = EXECUTE;
                    throw e;
                }
            }
            else if (stage == Stage.memory)
            {
                // detect if there is an exception in the memory operation in the mem stage (invalid address)
                if ((inst.mnem == Mnemonic.lw || inst.mnem == Mnemonic.sw) && !is_in_range_inc(inst.aluout, 0, DM.Count - 1))
                {
                    e.Source = MEMORY;
                    throw e;
                }
            }
            else if (stage == Stage.write_back)
            {
                // detect if there is an exception in the write back to the reg file in the wb stage (invalid reg or control signals)
                if (!isvalid_reg_ind(inst))
                {
                    e.Source = WRITEBACK;
                    throw e;
                }
            }

            return;
        }

        void InsertBubble(string source)
        {
            if (source == WRONG_PRED)
            {
                if (isbranch(executed2_in_EX_MIPS.mnem))
                {
                    if (executed2_in_EX_MIPS.prediction)
                    {
                        pcsrc = PCsrc.PCplus1;
                        PC = executed2_in_EX_MIPS.PC + 1;
                    }
                    else
                    {
                        pcsrc = PCsrc.pfc;
                        PC = executed2_in_EX_MIPS.PC + executed2_in_EX_MIPS.immeds;
                    }
                }
                fetched_in_IF_MIPS = fetch();
                IFID.mc = fetched_in_IF_MIPS;
                IDEX1 = GetNewInst();
                IDEX2 = GetNewInst();
                EXMEM = executed2_in_EX_MIPS;
                MEMWB = memed_in_MEM_MIPS;
            }
            else if (source == LOAD_USE)
            {
                IDEX1 = GetNewInst();
                IDEX2 = executed1_in_EX_MIPS;
                EXMEM = executed2_in_EX_MIPS;
                MEMWB = memed_in_MEM_MIPS;
            }
            else if (source == JR_INDECODE)
            {
                IFID.mc = "".PadLeft(32, '0');
                IDEX1 = decoded_in_ID_MIPS;
                IDEX2 = executed1_in_EX_MIPS;
                EXMEM = executed2_in_EX_MIPS;
                MEMWB = memed_in_MEM_MIPS;
            }
        }

        void ConsumeInst()
        {
            // here is the bulk of going through a complete cycle in the whole pipelined CPU
            try
            {
                write_back(MEMWB);
                if (hlt)
                    return;
                memed_in_MEM_MIPS = mem(ref EXMEM);
                executed2_in_EX_MIPS = execute2(IDEX2);
                executed1_in_EX_MIPS = execute1(IDEX1);
                decoded_in_ID_MIPS = decode(IFID.mc);
                fetched_in_IF_MIPS = fetch();
            }
            catch (Exception e)
            {
                if (e.Message == BUBBLE)
                {
                    InsertBubble(e.Source ?? "NO MESSAGE (ConsumeInst)");
                    return; // and then return and not fetch a new instruction
                }
                else if (e.Message == EXCEPTION)
                {
                    handle_exception(e);
                    throw e;
                }
            }
            MEMWB = memed_in_MEM_MIPS;
            EXMEM = executed2_in_EX_MIPS;
            IDEX2 = executed1_in_EX_MIPS;
            IDEX1 = decoded_in_ID_MIPS;
            IFID.mc = fetched_in_IF_MIPS;
        }
        int i = 0;
        public (int, Exceptions) Run()
        {
            Exceptions excep = Exceptions.NONE;

            while (PC < IM.Count)
            {

                i++;
                try
                {
                    ConsumeInst();
                }
                catch (Exception e)
                {
                    if (e.Message == EXCEPTION)
                    {
                        Run();
                        return (i, excep);
                    }
                }
                if (hlt)
                    return (i, excep);
                if (i == RISCV.MAX_CLOCKS)
                {
                    excep = Exceptions.INF_LOOP;
                    return (i, excep);
                }
            }
            return (i, excep);
        }
        public void print_regs()
        {
            RISCV.print_regs(regs);
        }
        public void print_DM()
        {
            RISCV.print_DM(DM);
        }
    }
    
    public class SingleCycle
    {
        public int PC;
        public bool hlt;
        public List<string> DM; // Data Mem
        public List<int> regs;
        public List<string> IM; // Instruction Mem

        public SingleCycle(List<string> insts, List<string> data_mem_init)
        {
            (IM, DM, regs) = InitMipsCPU(insts, data_mem_init);

            PC = 0;
            hlt = false;
        }
        Instruction decodemc(string mc, int pc)
        {
            Instruction inst = GetNewInst();
            inst.mc = mc;
            inst.PC = pc;
            inst.opcode = mc.Substring(mc.Length - (1 + 31), 6);
            inst.rsind = Convert.ToInt32(mc.Substring(mc.Length - (1 + 25), 5), 2);
            inst.rtind = Convert.ToInt32(mc.Substring(mc.Length - (1 + 20), 5), 2);
            inst.rdind = Convert.ToInt32(mc.Substring(mc.Length - (1 + 15), 5), 2);
            inst.shamt = mc.Substring(mc.Length - (1 + 10), 5);
            inst.funct = mc.Substring(mc.Length - (1 + 5), 6);
            inst.immeds = Convert.ToInt32(sx(mc.Substring(mc.Length - (1 + 15), 16)), 2);
            inst.immedz = Convert.ToInt32(zx(mc.Substring(mc.Length - (1 + 15), 16)), 2);
            inst.address = Convert.ToInt32(zx(mc.Substring(mc.Length - (1 + 25), 26)), 2);
            // riscv integer instruction map for formats (R, I, J)
            inst.rs = regs[inst.rsind];
            inst.rt = regs[inst.rtind];


            if (inst.opcode != "000000")
                inst.funct = "000000";
            if (!mnemonicmap.TryGetValue(inst.opcode + inst.funct, out Mnemonic value))
            {
                Exception e = new Exception(EXCEPTION)
                {
                    Source = DECODE
                };
                throw e;
            }
            else
            {
                if (mc == "".PadLeft(32, '0'))
                    inst.mnem = Mnemonic.nop;
                inst.mnem = value;
            }

            if (inst.mnem == Mnemonic.sw)
            {

            }

            inst.aluop = get_inst_aluop(inst.mnem);
            inst.format = get_format(inst.mnem);
            inst.oper1 = get_oper1(inst);
            inst.oper2 = get_oper2(inst);
            if (inst.format == "I")
            {
                inst.rdind = inst.rtind;
            }
            if (inst.format == "J")
            {
                inst.rdind = 31;
            }
            return inst;
        }

        void mem(ref Instruction inst)
        {
            if (inst.mnem == Mnemonic.lw)
            {
                if (!is_in_range_inc(inst.aluout, 0, DM.Count - 1))
                {
                    Exception e = new Exception($"Memory address {inst.aluout} is an invalid memory address")
                    {
                        Source = "SingleCycle::DataMemory"
                    };
                    throw e;
                }
                string smemout = DM[inst.aluout];
                inst.memout = Convert.ToInt32(smemout);
            }
            else if (inst.mnem == Mnemonic.sw)
            {
                string memin = Convert.ToString(inst.rt);
                if (!is_in_range_inc(inst.aluout, 0, DM.Count - 1))
                {
                    Exception e = new Exception($"Memory address {inst.aluout} is an invalid memory address")
                    {
                        Source = "SingleCycle::DataMemory"
                    };
                    throw e;
                }
                DM[inst.aluout] = memin;
            }
        }
        void write_back(Instruction inst)
        {
            if (inst.mnem == Mnemonic.hlt)
                hlt = true;

            if (iswb(inst.mnem) && inst.rdind != 0)
            {
                if (!is_in_range_inc(inst.rdind, 0, 31))
                {
                    Exception e = new Exception($"register index {inst.rdind} is an invalid index")
                    {
                        Source = "SingleCycle::write_back"
                    };
                    throw e;
                }
                regs[inst.rdind] = (inst.mnem == Mnemonic.lw) ? inst.memout : inst.aluout;
                //Console.WriteLine($"writereg = {inst.rdind} , writedata = {regs[inst.rdind]}");
            }

        }
        void ConsumeInst()
        {
            Instruction inst = new();
            try
            {
                // fetching
                //Console.WriteLine($"PC = {PC}");
                string mc = IM[PC];
                // decoding
                inst = decodemc(mc, PC);
                // executing
                inst.aluout = execute_inst(inst);
                // mem RISCV
                mem(ref inst);
                // writing back
                write_back(inst);
            }
            catch (Exception e)
            {
                PC = HANDLER_ADDR;

                Run();
                throw e;
            }

            if (hlt)
                return;
            // updating the PC
            if (inst.format == "J")
            {
                PC = inst.address;
            }
            else if (inst.mnem == Mnemonic.jr)
            {
                PC = inst.aluout;
            }
            else if (isbranch(inst.mnem) && isbranch_taken(inst))
            {
                PC += inst.immeds;
            }
            else
                PC += 1;
        }

        public (int, Exceptions) Run()
        {
            int i = 0;
            while (PC < IM.Count)
            {
                i++;
                try
                {
                    ConsumeInst();
                }
                catch
                {
                    //Console.WriteLine($"cycles consumed = {i}");
                    i--;
                    return (i, Exceptions.EXCEPTION);
                }
                if (hlt)
                    return (i, Exceptions.NONE);
                if (i == RISCV.MAX_CLOCKS)
                {
                    return (0, Exceptions.INF_LOOP);
                }
            }
            return (i, Exceptions.NONE);
        }
        public void print_regs()
        {
            RISCV.print_regs(regs);
        }
        public void print_DM()
        {
            RISCV.print_DM(DM);
        }
    }
    }
