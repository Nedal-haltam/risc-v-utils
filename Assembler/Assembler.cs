


using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Text;

namespace Assembler
{
    public enum InstructionType
    {
        rtype, itype, jtype
    }

    public struct Token
    {
        public string m_value;
        public Token()
        {
            m_value = "";
        }
        public Token(string value)
        {
            m_value = value;
        }
    }

    public struct Instruction
    {
        public List<Token> m_tokens;
        public InstructionType m_type;
        public Instruction()
        {
            m_tokens = [];
        }
        public Instruction(List<Token> tokens)
        {
            m_tokens = tokens;
        }
    }

    public struct Program
    {
        public List<Instruction> instructions;
        public List<string> mc;

        public Program()
        {
            instructions = [];
            mc = [];
        }
    }

    public class Assembler
    {
        public static void Assert(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(msg);
            Console.ResetColor();
            Environment.Exit(1);
        }

        public bool lblinvlabel = false;
        public bool lblmultlabels = false;
        public bool lblINVINST = false;
        readonly Dictionary<string, int> labels = [];
        List<string> m_prog = [];
        string m_curr_inst = "";
        int m_curr_index = 0;
        int curr_inst_index;
        readonly List<string> REG_LIST =
        [ "zero", "at", "v0", "v1", "a0", "a1", "a2", "a3", "t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
          "s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7", "t8", "t9", "k0", "k1", "gp", "sp", "fp", "ra"];
        // The instruction type is the main token in a given instruction and should be known the first token in an instruction (first word)
        // the opcodes is a dictionary the you give it a certain opcode (in words) and get beack the binaries (or machine code) corresponding to that opcode
        // take a look to know what to expect because the binary might be different depending on the opcode some of them only opcode or with func3 or even with func7
        readonly Dictionary<string, string> opcodes = new()//{ "inst"     , "opcode/funct" },
        {
            { "nop"  , "000000" },
            { "hlt"  , "111111" },

            // R-format , opcode = 0
            { "add"  , "100000" },
            { "mul"  , "101000" },
            { "addu" , "100001" },
            { "sub"  , "100010" },
            { "subu" , "100011" },
            { "and"  , "100100" },
            { "or"   , "100101" },
            { "xor"  , "100110" },
            { "nor"  , "100111" },
            { "slt"  , "101010" },
            { "seq"  , "101100" },
            { "sne"  , "101101" },
            { "sgt"  , "101011" },
            { "sll"  , "000000" },
            { "srl"  , "000010" },
            { "jr"   , "001000" },
            
            // I-format
            { "addi" , "001000" },
            { "andi" , "001100" },
            { "ori"  , "001101" },
            { "xori" , "001110" },
            { "slti" , "101010" },
            { "lw"   , "100011" },
            { "sw"   , "101011" },
            { "beq"  , "000100" },
            { "bne"  , "000101" }, 
        
            // J-format
            { "j"    , "000010" },
            { "jal"  , "000011" },

        };
        InstructionType? GetInstType(string name)
        {
            return name switch
            {
                "add" or "addu" or "sub" or "subu" or "and" or "or" or "xor" or "nor" or "slt" or "seq" or "sne" or 
                "sgt" or "sll" or "srl" or "jr" or "nop" or "mul" => InstructionType.rtype,
                "addi" or "andi" or "ori" or "xori" or "slti" or "lw" or "sw" or "beq" or "bne" or "hlt" => InstructionType.itype,
                                                                                                                  
                "j" or "jal" => InstructionType.jtype,
                _ => null,
            };
        }

        string? Getregindex(string reg)
        {
            if (reg.StartsWith('$'))
            {

                string name = reg[1..];
                if (byte.TryParse(name, out byte usb) && usb >= 0 && usb <= 31)
                {
                    return Convert.ToString(usb, 2).PadLeft(5, '0');
                }
                else
                {
                    if (!REG_LIST.Contains(name))
                    {
                        return null;
                    }
                    return Convert.ToString(REG_LIST.IndexOf(name), 2).PadLeft(5, '0');
                }
            }
            else
                return null;

        }
        string? Getrtypeinst(Instruction inst)
        {
            string mc;

            if (inst.m_tokens[0].m_value == "jr") // jr x12
            {
                if (inst.m_tokens.Count != 2)
                    return null;
                string? rs1 = Getregindex(inst.m_tokens[1].m_value);
                if (rs1 == null) return null;
                mc = "000000" + rs1 + "000000000000000" + opcodes[inst.m_tokens[0].m_value];
            }
            else
            {
                if (inst.m_tokens.Count != 4)
                    return null;
                string? rd = Getregindex(inst.m_tokens[1].m_value);
                string? rs1 = Getregindex(inst.m_tokens[2].m_value);
                string funct = opcodes[inst.m_tokens[0].m_value];

                if (rd == null || rs1 == null) return null;

                if (inst.m_tokens[0].m_value == "sll" || inst.m_tokens[0].m_value == "srl") // sll x1, x2, 2
                {
                    string shamt = inst.m_tokens[3].m_value;
                    if (byte.TryParse(shamt, out byte usb))
                    {
                        shamt = Convert.ToString(usb, 2);
                        if (shamt.Length > 5) shamt = shamt.Substring(shamt.Length - 5, 5);
                        shamt = shamt.PadLeft(5, '0');
                    }
                    else if (sbyte.TryParse(shamt, out sbyte sb))
                    {
                        shamt = Convert.ToString(sb, 2);
                        if (shamt.Length > 5) shamt = shamt.Substring(shamt.Length - 5, 5);
                        shamt = shamt.PadLeft(5, '0');
                    }
                    else
                        return null;
                    mc = "000000"
                        + "00000"
                        + rs1
                        + rd
                        + shamt
                        + funct;
                }
                else
                {
                    string? rs2 = Getregindex(inst.m_tokens[3].m_value);
                    if (rs2 == null)
                        return null;
                    mc = "000000"
                        + rs1
                        + rs2
                        + rd
                        + "00000"
                        + funct;
                }
            } 
            return mc;
        }
        string? GetImmed(string immed)
        {
            // andi, ori, xori (they do zero extend)
            if ((immed.StartsWith("0x") || immed.StartsWith("0X")))
            {
                short temp;
                try { temp = Convert.ToInt16(immed, 16); }
                catch { return null; }

                return Convert.ToString(temp, 2).PadLeft(16, '0');
            }
            else if (ushort.TryParse(immed, out ushort usb))
            {
                immed = Convert.ToString(usb, 2);
                return immed.PadLeft(16, '0');
            }
            else if (short.TryParse(immed, out short sb))
            {
                immed = Convert.ToString(sb, 2);
                return immed.PadLeft(16, immed[0]);
            }
            else
                return null;
        }
        string? Getitypeinst(Instruction inst)
        {
            if (inst.m_tokens.Count > 0 && (inst.m_tokens[0].m_value == "sw" || inst.m_tokens[0].m_value == "lw") && inst.m_tokens.Count != 6)
                return null;
            else if (!(inst.m_tokens[0].m_value == "sw" || inst.m_tokens[0].m_value == "lw") && inst.m_tokens.Count != 4)
                return null;
            string mc;
            string opcode = opcodes[inst.m_tokens[0].m_value];

            if (Isbranch(inst.m_tokens[0].m_value))
            {
                string? reg1 = Getregindex(inst.m_tokens[1].m_value);
                string? reg2 = Getregindex(inst.m_tokens[2].m_value);
                if (reg1 == null || reg2 == null)
                    return null;
                if (!labels.ContainsKey(inst.m_tokens[3].m_value))
                    return null;
                string immed = inst.m_tokens[3].m_value;
                immed = (labels[immed] - curr_inst_index).ToString();
                if (ushort.TryParse(immed, out ushort usb))
                    immed = Convert.ToString(usb, 2).PadLeft(16, '0');
                else if (short.TryParse(immed, out short sb))
                {
                    immed = Convert.ToString(sb, 2);
                    immed = immed.PadLeft(16, immed[0]);
                }
                else
                    return null;
                string rs1 = reg1;
                string rs2 = reg2;
                mc = opcode + rs1 + rs2 + immed;
            }
            else if (inst.m_tokens[0].m_value == "lw" || inst.m_tokens[0].m_value == "sw")
            { // sw $1, 0($1)
                string? reg1 = Getregindex(inst.m_tokens[1].m_value);
                string? reg2 = Getregindex(inst.m_tokens[4].m_value);
                if (reg1 == null || reg2 == null)
                    return null;
                string? immed = GetImmed(inst.m_tokens[2].m_value);
                if (immed == null)
                    return null;
                string rd = reg1;
                string rs1 = reg2;
                mc = opcode + rs1 + rd + immed;
            }
            else
            {
                string? reg1 = Getregindex(inst.m_tokens[1].m_value);
                string? reg2 = Getregindex(inst.m_tokens[2].m_value);
                if (reg1 == null || reg2 == null)
                    return null;
                string? immed = GetImmed(inst.m_tokens[3].m_value);
                if (immed == null)
                    return null;
                string rd = reg1;
                string rs1 = reg2;
                mc = opcode + rs1 + rd + immed;
            }

            return mc;
        }
        string? Getjtypeinst(Instruction inst)
        {
            if (inst.m_tokens.Count != 2 || !labels.TryGetValue(inst.m_tokens[1].m_value, out int lbl))
                return null;
            string immed = Convert.ToString(lbl, 2);
            immed = immed.PadLeft(26, '0');
            string mc = opcodes[inst.m_tokens[0].m_value] + immed;
            return mc;
        }
        List<string>? GetMachineCodeOfProg(ref Program program)
        {
            List<string> mcs = [];
            curr_inst_index = 0;
            for (int i = 0; i < program.instructions.Count; i++)
            {
                InstructionType? type = GetInstType(program.instructions[i].m_tokens[0].m_value);
                if (type.HasValue)
                {
                    Instruction temp = program.instructions[i];
                    temp.m_type = type.Value;
                    program.instructions[i] = temp;
                    string? mc = GetMcOfInst(program.instructions[i]);
                    if (mc == null)
                    {
                        return null;
                    }
                    mcs.Add(mc);
                    curr_inst_index++;
                }
                else
                {
                    return null;
                }
            }

            return mcs;
        }
        string? GetMcOfInst(Instruction inst)
        {
            if (inst.m_tokens.Count > 0 && (inst.m_tokens[0].m_value == "hlt" || inst.m_tokens[0].m_value == "nop"))
                return opcodes[inst.m_tokens[0].m_value].PadRight(32, '0');
            // here we construct the binaries of a given instruction
            return inst.m_type switch
            {
                InstructionType.rtype => Getrtypeinst(inst),
                InstructionType.itype => Getitypeinst(inst),
                InstructionType.jtype => Getjtypeinst(inst),
                _ => null,
            };
        }
        public bool Isbranch(string mnem)
        {
            return mnem == "beq" || mnem == "bne";
        }
        bool Is_pseudo_branch(string mnem)
        {
            return mnem == "bltz" || mnem == "bgez";
        }
        bool Is_pseudo(Instruction inst)
        {
            return Is_pseudo_branch(inst.m_tokens[0].m_value)/* || other pseudo insts*/;
        }
        List<Instruction> GetPseudo(Instruction inst)
        {
            if (Is_pseudo_branch(inst.m_tokens[0].m_value) && inst.m_tokens.Count == 3)
            {
                string branch = "";
                if (inst.m_tokens[0].m_value == "bltz")
                {
                    branch = "bne";
                }
                else if (inst.m_tokens[0].m_value == "bgez")
                {
                    branch = "beq";
                }
                return [
                    new Instruction([new Token("slt"), new Token("$at"), new Token($"{inst.m_tokens[1].m_value}"), new Token("$0") ]),
                    new Instruction([new Token(branch), new Token("$at"), new Token("$0"), new Token($"{inst.m_tokens[2].m_value}") ]),
                ];
            }
            lblINVINST = true;
            //Assert($"Invalid Pseudo Instruction : {inst.m_tokens[0].m_value}");
            return [];
        }
        void SubstitutePseudoInProg(ref Program program)
        {
            for (int i = 0; i < program.instructions.Count; i++)
            {
                if (Is_pseudo(program.instructions[i]))
                {
                    List<Instruction> replace = GetPseudo(program.instructions[i]);
                    program.instructions.RemoveAt(i);
                    program.instructions.InsertRange(i, replace);
                    i = 0;
                }
            }
        }


        char? peek(int offset = 0)
        {
            if (m_curr_index + offset < m_curr_inst.Length)
            {
                return m_curr_inst[m_curr_index + offset];
            }
            return null;
        }
        char? peek(char type, int offset = 0)
        {
            char? token = peek(offset);
            if (token.HasValue && token.Value == type)
            {
                return token;
            }
            return null;
        }
        char consume()
        {
            return m_curr_inst.ElementAt(m_curr_index++);
        }
        bool IsComment()
        {
            return (peek('/').HasValue && peek('/', 1).HasValue) || peek('#').HasValue;
        }

        string? Is_valid_label(Instruction label)
        {
            if (label.m_tokens.Count > 0 && label.m_tokens[0].m_value.Contains(':'))
            {
                return label.m_tokens[0].m_value[..^1];
            }
            else if (label.m_tokens.Count > 1 && label.m_tokens[1].m_value.Contains(':'))
            {
                return label.m_tokens[0].m_value;
            }
            else
                return null;
        }
        private void Subtitute_labels(ref Program program)
        {
            int index = 0;
            for (int i = 0; i < program.instructions.Count; i++)
            {
                Instruction inst = program.instructions[i];
                if (inst.m_tokens.Any(token => token.m_value.Contains(':')))
                {
                    string? label = Is_valid_label(program.instructions[i]);
                    lblinvlabel |= label == null;
                    if (label != null)
                    {
                        if (!labels.TryAdd(label, index))
                            lblmultlabels |= true;
                    }
                    for (int j = 0; j < inst.m_tokens.Count; j++)
                    {
                        if (inst.m_tokens[j].m_value.Contains(':') && j != inst.m_tokens.Count - 1)
                        {
                            index++;
                            break;
                        }
                    }
                }
                else
                    index++;
            }
            for (int i = 0; i < program.instructions.Count; i++)
            {
                int indexx = -1;
                for (int j = 0; j < program.instructions[i].m_tokens.Count; j++)
                {
                    if (program.instructions[i].m_tokens[j].m_value.Contains(':'))
                    {
                        indexx = j;
                        break;
                    }
                }
                if (indexx != -1)
                {
                    program.instructions[i].m_tokens.RemoveRange(0, indexx + 1);
                }
            }
            program.instructions.RemoveAll(inst => inst.m_tokens.Count == 0);
        }
        
        Instruction? TokenizeInst()
        {
            StringBuilder buffer = new StringBuilder();
            Instruction instruction = new Instruction();
            while (peek().HasValue)
            {
                char c = peek().Value;

                if (char.IsWhiteSpace(c) || c == ',')
                {
                    if (buffer.Length > 0)
                    {
                        instruction.m_tokens.Add(new Token(buffer.ToString()));
                        buffer.Clear();
                    }
                    consume();
                }
                else if (IsComment())
                {
                    if (buffer.Length > 0)
                    {
                        instruction.m_tokens.Add(new Token(buffer.ToString()));
                        buffer.Clear();
                    }
                    break;
                }
                else if (c == '+')
                {
                    consume();
                    string a = buffer.ToString();
                    buffer.Clear();
                    while(peek().HasValue && !peek('(').HasValue)
                    {
                        buffer.Append(consume());
                    }
                    string b = buffer.ToString();
                    buffer.Clear();
                    string sum = (Convert.ToInt32(a) + Convert.ToInt32(b)).ToString();
                    instruction.m_tokens.Add(new Token(sum));
                }
                else if (c == '(' || c == ')')
                {
                    if (buffer.Length != 0)
                    {
                        instruction.m_tokens.Add(new Token(buffer.ToString()));
                        buffer.Clear();
                    }
                    buffer.Append(char.ToLower(c));
                    instruction.m_tokens.Add(new Token(buffer.ToString()));
                    consume();
                    buffer.Clear();
                }
                else if (c == ':')
                {
                    buffer.Append(char.ToLower(c));
                    if (buffer.Length > 0)
                    {
                        instruction.m_tokens.Add(new Token(buffer.ToString()));
                        buffer.Clear();
                    }
                    consume();

                }
                else
                {
                    buffer.Append(char.ToLower(c));
                    consume();
                }
            }
            if (buffer.Length > 0)
            {
                instruction.m_tokens.Add(new Token(buffer.ToString()));
                buffer.Clear();
            }
            m_curr_index = 0;
            return instruction;
        }
        Program? TokenizeProg(List<string> thecode)
        {
            Program program = new Program();
            for (int i = 0; i < thecode.Count; i++)
            {
                m_curr_inst = thecode[i];
                Instruction? instruction = TokenizeInst();
                if (instruction.HasValue)
                {
                    program.instructions.Add(instruction.Value);
                }
                else
                {
                    return null;
                }
            }

            return program;
        }
        public Program? ASSEMBLE(List<string> in_prog)
        {
            lblINVINST = false;
            lblinvlabel = false;
            lblmultlabels = false;
            labels.Clear();

            in_prog.RemoveAll(line => string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line));
            m_prog = in_prog;
            m_prog.Add("HLT");
            Program? prog = TokenizeProg(m_prog);

            if (prog.HasValue)
            {
                Program program = prog.Value;
                SubstitutePseudoInProg(ref program);
                Subtitute_labels(ref program);
                List<string>? mc = GetMachineCodeOfProg(ref program);
                if (mc != null)
                {
                    program.mc = mc;
                }
                else
                {
                    lblINVINST = true;
                    return null;
                }
                return program;
            }
            else
            {
                return null;
            }
        }

        public List<string> GetInstsAsText(Program program)
        {
            List<string> ret = [];
            for (int i = 0; i < program.instructions.Count; i++)
            {
                Instruction instruction = program.instructions[i];
                string mnem = instruction.m_tokens[0].m_value;
                if (mnem == "beq" || mnem == "bne")
                {
                    string LabelValue = Convert.ToInt16(program.mc[i].Substring(16), 2).ToString();
                    instruction.m_tokens[^1] = new Token(LabelValue);
                }
                string inst = "";
                instruction.m_tokens.ForEach(token => inst += token.m_value + " ");
                ret.Add(inst);
            }
            return ret;
        }
    }
}
