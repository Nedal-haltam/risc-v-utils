
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;

namespace Assembler
{
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
        private readonly Dictionary<string, int> labels = [];
        List<string> m_prog = [];
        private string m_curr_inst = "";
        private int m_curr_index = 0;
        public readonly Dictionary<string, int> REG_LIST = new()
        {
            {"zero", 0},
            {"ra"  , 1},
            {"sp"  , 2},
            {"gp"  , 3},
            {"tp"  , 4},
            {"t0"  , 5},
            {"at"  , 5},
            {"t1"  , 6},
            {"t2"  , 7},
            {"s0"  , 8},
            {"s1"  , 9},
            {"a0"  , 10},
            {"a1"  , 11},
            {"a2"  , 12},
            {"a3"  , 13},
            {"a4"  , 14},
            {"a5"  , 15},
            {"a6"  , 16},
            {"a7"  , 17},
            {"s2"  , 18},
            {"s3"  , 19},
            {"s4"  , 20},
            {"s5"  , 21},
            {"s6"  , 22},
            {"s7"  , 23},
            {"s8"  , 24},
            {"s9"  , 25},
            {"s10" , 26},
            {"s11" , 27},
            {"t3"  , 28},
            {"t4"  , 29},
            {"t5"  , 30},
            {"t6"  , 31},
        };
        public struct InstInfo(string Opcode, string Funct3, string Funct7)
        {
            public string Opcode = Opcode;
            public string Funct3 = Funct3;
            public string Funct7 = Funct7;
        }
        public static class INSTRUCTIONS
        {
            readonly static Dictionary<string, InstInfo> Infos = new()
            {
                {"lui"   , new("0110111", "", "")},
                {"auipc" , new("0010111", "", "")},

                {"addi"  , new("0010011", "000", "")},
                {"slti"  , new("0010011", "010", "")},
                {"sltiu" , new("0010011", "011", "")},
                {"xori"  , new("0010011", "100", "")},
                {"ori"   , new("0010011", "110", "")},
                {"andi"  , new("0010011", "111", "")},
                {"slli"  , new("0010011", "001", "")},
                {"srli"  , new("0010011", "101", "")},
                {"srai"  , new("0010011", "101", "")},

                {"add"   , new("0110011", "000", "0000000")},
            };
            public static string GetRtypeInst(string mnem, string rs1, string rs2, string rd)
            {
                if (!Infos.ContainsKey(mnem))
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
                InstInfo info = Infos[mnem];
                return info.Funct7 + rs2 + rs1 + info.Funct3 + rd + info.Opcode;
            }
            public static string GetItypeInst(string mnem, string imm12, string rs1, string rd)
            {
                if (!Infos.ContainsKey(mnem))
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
                InstInfo info = Infos[mnem];
                return imm12 + rs1 + info.Funct3 + rd + info.Opcode;
            }
            public static string GetStypeInst(string mnem, string imm12, string rs1, string rs2)
            {
                if (!Infos.ContainsKey(mnem))
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
                InstInfo info = Infos[mnem];
                return imm12.Substring(0, 7) + rs2 + rs1 + info.Funct3 + imm12.Substring(7, 5) + info.Opcode;
            }
            public static string GetUtypeInst(string mnem, string imm20, string rd)
            {
                if (!Infos.ContainsKey(mnem))
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
                return imm20 + rd + Infos[mnem].Opcode;
            }
        }
        string Getregindex(string reg)
        {
            if (reg.StartsWith('$') || reg.StartsWith('x'))
                reg = reg[1..];

            if (REG_LIST.TryGetValue(reg, out int index))
            {
                return Convert.ToString(index, 2).PadLeft(5, '0');
            }
            else if (byte.TryParse(reg, out byte usb) && usb >= 0 && usb <= 31)
            {
                return Convert.ToString(usb, 2).PadLeft(5, '0');
            }
            else
            {
                Shartilities.Log(Shartilities.LogType.ERROR, $"invalid register `{reg}`\n", 1);
                return "";
            }
        }
        void Check(string mnem, int have, int want) => Shartilities.Assert(have == want, $"invalid `{mnem}` instruction");
        List <string> GetMcOfInst(Instruction inst)
        {
            // references:
            // - https://msyksphinz-self.github.io/riscv-isadoc/
            // - https://riscv.github.io/riscv-isa-manual/snapshot/unprivileged/
            Shartilities.TODO($"assemble here, my friend");
            List<Token> ts = inst.m_tokens;
            string mnem = ts[0].m_value;
            // TODO: support negative numbers
            switch (mnem)
            {
                case "lui": 
                    {
                        // lui rd,imm
                        // x[rd] = sext(immediate[31:12] << 12)
                        Check(mnem, ts.Count, 3);
                        string rd = Getregindex(ts[1].m_value);
                        string imm = ts[2].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0')[..20];
                        return [INSTRUCTIONS.GetUtypeInst(mnem, imm, rd)];
                    }
                case "auipc": 
                    {
                        // auipc rd,imm
                        // x[rd] = pc + sext(immediate[31:12] << 12)
                        Check(mnem, ts.Count, 3);
                        string rd = Getregindex(ts[1].m_value);
                        string imm = ts[2].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 19, 20);
                        return [INSTRUCTIONS.GetUtypeInst(mnem, imm, rd)];
                    }
                case "addi":
                    {
                        // addi rd,rs1,imm
                        // x[rd] = x[rs1] + sext(immediate)
                        Check(mnem, ts.Count, 4);
                        string rd = Getregindex(ts[1].m_value);
                        string rs1 = Getregindex(ts[2].m_value);
                        string imm = ts[3].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "mv":
                    {
                        // mv rd,rs1
                        // x[rd] = x[rs1]
                        Check(mnem, ts.Count, 3);
                        string rs1 = Getregindex(ts[2].m_value);
                        string rd = Getregindex(ts[1].m_value);
                        return [INSTRUCTIONS.GetItypeInst("addi", "0".PadLeft(12, '0'), rs1, rd)];
                    }
                case "slti":
                    {
                        // slti rd,rs1,imm
                        // x[rd] = x[rs1] <s sext(immediate)
                        // x[rd] = (signed(x[rs1]) < signed(sext(immediate))) ? 1 : 0
                        Check(mnem, ts.Count, 4);
                        string rd = Getregindex(ts[1].m_value);
                        string rs1 = Getregindex(ts[2].m_value);
                        string imm = ts[3].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "sltiu":
                    {
                        // sltiu rd,rs1,imm
                        // x[rd] = x[rs1] <u sext(immediate)
                        // the difference is that the numbers are treated as unsigned instead
                        // x[rd] = (unsigned(x[rs1]) < unsigned(sext(immediate))) ? 1 : 0
                        Check(mnem, ts.Count, 4);
                        string rd = Getregindex(ts[1].m_value);
                        string rs1 = Getregindex(ts[2].m_value);
                        string imm = ts[3].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "xori":
                    {
                        // xori rd,rs1,imm
                        // x[rd] = x[rs1] ^ sext(immediate)
                        Check(mnem, ts.Count, 4);
                        string rd = Getregindex(ts[1].m_value);
                        string rs1 = Getregindex(ts[2].m_value);
                        string imm = ts[3].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "not":
                    {
                        // not rd,rs1
                        // x[rd] = x[rs1] ^ sext(-1)
                        Check(mnem, ts.Count, 3);
                        string rs1 = Getregindex(ts[2].m_value);
                        string rd = Getregindex(ts[1].m_value);
                        return [INSTRUCTIONS.GetItypeInst("xori", "1".PadLeft(12, '1'), rs1, rd)];
                    }
                case "ori":
                    {
                        // ori rd,rs1,imm
                        // x[rd] = x[rs1] | sext(immediate)
                        Check(mnem, ts.Count, 4);
                        string rd = Getregindex(ts[1].m_value);
                        string rs1 = Getregindex(ts[2].m_value);
                        string imm = ts[3].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "andi":
                    {
                        // andi rd,rs1,imm
                        // x[rd] = x[rs1] & sext(immediate)
                        Check(mnem, ts.Count, 4);
                        string rd = Getregindex(ts[1].m_value);
                        string rs1 = Getregindex(ts[2].m_value);
                        string imm = ts[3].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "slli":
                    {
                        // slli rd,rs1,shamt
                        // x[rd] = x[rs1] << shamt
                        // NOTE: in RV64, bit-25 in the instruction maching code is used to shamt[5] so the shamt in this case is 6-bits,
                        // but in RV32 the shamt is 5-bits
                        Check(mnem, ts.Count, 4);
                        string rd = Getregindex(ts[1].m_value);
                        string rs1 = Getregindex(ts[2].m_value);
                        string imm = ts[3].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 4, 5).PadLeft(12, '0');
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "srli":
                    {
                        // srli rd,rs1,shamt
                        // x[rd] = x[rs1] >>u shamt
                        // NOTE: in RV64, bit-25 in the instruction maching code is used to shamt[5] so the shamt in this case is 6-bits,
                        // but in RV32 the shamt is 5-bits
                        Check(mnem, ts.Count, 4);
                        string rd = Getregindex(ts[1].m_value);
                        string rs1 = Getregindex(ts[2].m_value);
                        string imm = ts[3].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 4, 5).PadLeft(12, '0');
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "srai":
                    {
                        // srai rd,rs1,shamt
                        // x[rd] = x[rs1] >>s shamt
                        // NOTE: in RV64, bit-25 in the instruction maching code is used to shamt[5] so the shamt in this case is 6-bits,
                        // but in RV32 the shamt is 5-bits
                        Check(mnem, ts.Count, 4);
                        string rd = Getregindex(ts[1].m_value);
                        string rs1 = Getregindex(ts[2].m_value);
                        string imm = ts[3].m_value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 4, 5).PadLeft(12, '0');
                        imm = imm.Substring(0, 1) + "1" + imm.Substring(2);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "add":
                    {
                        // add rd,rs1,rs2
                        // x[rd] = x[rs1] + x[rs2]
                        Check(mnem, ts.Count, 4);
                        string rd = Getregindex(ts[1].m_value);
                        string rs1 = Getregindex(ts[2].m_value);
                        string rs2 = Getregindex(ts[3].m_value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];

                    }
                default:
                    Shartilities.Log(Shartilities.LogType.ERROR, $"invalid instruction mnemonic `{mnem}`\n", 1);
                    return [];
            }
        }
        List<string> GetMachineCodeOfProg(ref Program program)
        {
            List<string> mcs = [];
            for (int i = 0; i < program.instructions.Count; i++)
            {
                mcs.AddRange(GetMcOfInst(program.instructions[i]));
            }
            return mcs;
        }
        List<Instruction> GetPseudo(Instruction inst)
        {
            string mnem = inst.m_tokens[0].m_value;
            if ((mnem == "bltz" || mnem == "bgez") && inst.m_tokens.Count == 3)
            {
                string branch = "";
                if (mnem == "bltz")
                {
                    branch = "bne";
                }
                else if (mnem == "bgez")
                {
                    branch = "beq";
                }
                return [
                    new([new("slt"), new("$at"), new($"{inst.m_tokens[1].m_value}"), new("$0") ]),
                    new([new(branch), new("$at"), new("$0"), new($"{inst.m_tokens[2].m_value}") ]),
                ];
            }
            else if (mnem == "li" && inst.m_tokens.Count == 3)
            {
                return [
                    new([new("ori"), new(inst.m_tokens[1].m_value), new("zero"), new(inst.m_tokens[2].m_value)])
                ];
            }
            else if (mnem == "la" && inst.m_tokens.Count == 3)
            {
                return [
                    new([new("ori"), new(inst.m_tokens[1].m_value), new("zero"), new(inst.m_tokens[2].m_value)])
                ];
            }
            else if (mnem == "call")
            {
                return [
                    new([new("jal"), new(inst.m_tokens[1].m_value)])
                ];
            }
            else if (mnem == "mv")
            {
                return [
                    new([new("or"), new(inst.m_tokens[1].m_value), new(inst.m_tokens[2].m_value), new("zero")])
                ];
            }
            else if (mnem == "ret")
            {
                return [
                    new([new("jr"), new("ra")])
                ];
            }
            else
            {
                Shartilities.Log(Shartilities.LogType.ERROR, $"invalid pseudo instruction `{inst.m_tokens[0].m_value}`\n", 1);
                return [];
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

        string Is_valid_label(Instruction label)
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
            {
                Shartilities.Log(Shartilities.LogType.ERROR, $"invalid label syntax in label `{label.m_tokens[0]}`\n", 1);
                return "";
            }
        }
        private void Subtitute_labels(ref Program program)
        {
            int index = 0;
            for (int i = 0; i < program.instructions.Count; i++)
            {
                Instruction inst = program.instructions[i];
                if (inst.m_tokens.Any(token => token.m_value.Contains(':')))
                {
                    string label = Is_valid_label(program.instructions[i]);
                    if (!labels.TryAdd(label, index))
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid label `{label}`\n", 1);

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
        Instruction TokenizeInst()
        {
            StringBuilder buffer = new();
            Instruction instruction = new();
            while (peek().HasValue)
            {
                char? t = peek();
                char c;
                if (t.HasValue)
                    c = t.Value;
                else break;

                if (char.IsWhiteSpace(c) || c == ',')
                {
                    consume();
                    if (buffer.Length > 0)
                    {
                        instruction.m_tokens.Add(new(buffer.ToString()));
                        buffer.Clear();
                    }
                }
                else if (IsComment())
                {
                    if (buffer.Length > 0)
                    {
                        instruction.m_tokens.Add(new(buffer.ToString()));
                        buffer.Clear();
                    }
                    break;
                }
                else if (c == '(' || c == ')')
                {
                    if (buffer.Length != 0)
                    {
                        instruction.m_tokens.Add(new(buffer.ToString()));
                        buffer.Clear();
                    }
                    buffer.Append(char.ToLower(c));
                    instruction.m_tokens.Add(new(buffer.ToString()));
                    consume();
                    buffer.Clear();
                }
                else if (c == ':')
                {
                    consume();
                    buffer.Append(char.ToLower(c));
                    if (buffer.Length > 0)
                    {
                        instruction.m_tokens.Add(new(buffer.ToString()));
                        buffer.Clear();
                    }
                }
                else
                {
                    buffer.Append(char.ToLower(c));
                    consume();
                }
            }
            if (buffer.Length > 0)
            {
                instruction.m_tokens.Add(new(buffer.ToString()));
                buffer.Clear();
            }
            m_curr_index = 0;
            return instruction;
        }
        Program TokenizeProg(List<string> thecode)
        {
            Program program = new();
            for (int i = 0; i < thecode.Count; i++)
            {
                m_curr_inst = thecode[i];
                Instruction instruction = TokenizeInst();
                program.instructions.Add(instruction);
            }

            return program;
        }
        public Program AssembleProgram(List<string> in_prog)
        {
            labels.Clear();
            in_prog.RemoveAll(line => string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line));
            m_prog = [.. in_prog];
            m_prog.Add("HLT");
            Program program = TokenizeProg(m_prog);
            Subtitute_labels(ref program);
            List<string> mc = GetMachineCodeOfProg(ref program);
            program.mc = mc;
            return program;
        }
        static int LineNumber([System.Runtime.CompilerServices.CallerLineNumber] int LineNumber = 0)
        {
            return LineNumber;
        }
        static string FilePath([System.Runtime.CompilerServices.CallerFilePath] string FilePath = "")
        {
            return FilePath;
        }
    }
}
