
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
        public bool lblinvlabel = false;
        public bool lblmultlabels = false;
        public bool lblINVINST = false;
        readonly Dictionary<string, int> labels = [];
        List<string> m_prog = [];
        string m_curr_inst = "";
        int m_curr_index = 0;
        int curr_inst_index;
        readonly Dictionary<string, int> REG_LIST = new()
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
        readonly Dictionary<string, string> opcodes = new()//{ "inst"     , "`opcode` or `funct`" },
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
        InstructionType GetInstType(string name)
        {
            switch (name)
            {
                case "add":
                case "addu":
                case "sub":
                case "subu":
                case "and":
                case "or":
                case "xor":
                case "nor":
                case "slt":
                case "seq":
                case "sgt":
                case "sll":
                case "srl":
                case "jr":
                case "nop":
                case "sne":
                case "mul":
                    return InstructionType.rtype;
                case "addi":
                case "andi":
                case "ori":
                case "xori":
                case "slti":
                case "lw":
                case "sw":
                case "beq":
                case "bne":
                case "hlt":
                    return InstructionType.itype;
                case "j":
                case "jal":
                    return InstructionType.jtype;
                default:
                    Shartilities.Log(Shartilities.LogType.ERROR, $"no type for instruction `{name}`\n", 1);
                    return InstructionType.rtype;
            }
        }

        string Getregindex(string reg)
        {
            if (reg.StartsWith('$') || reg.StartsWith('x'))
            {
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
                    Shartilities.Log(Shartilities.LogType.ERROR, $"invalid register value `{reg}`\n", 1);
                    return "";
                }
            }
            else
            {
                if (!REG_LIST.ContainsKey(reg))
                {
                    Shartilities.Log(Shartilities.LogType.ERROR, $"invalid register name : `{reg}`\n", 1);
                }
                return Convert.ToString(REG_LIST[reg], 2).PadLeft(5, '0');
            }
        }
        string Getrtypeinst(Instruction inst)
        {
            string mc;

            if (inst.m_tokens[0].m_value == "jr") // jr x12
            {
                if (inst.m_tokens.Count != 2)
                    Shartilities.Log(Shartilities.LogType.ERROR, "invalid jr instruction\n", 1);
                string rs1 = Getregindex(inst.m_tokens[1].m_value);

                mc = "000000" + rs1 + "000000000000000" + opcodes[inst.m_tokens[0].m_value];
            }
            else
            {
                if (inst.m_tokens.Count != 4)
                    Shartilities.Log(Shartilities.LogType.ERROR, $"invlid R-Type instruction\n", 1);
                string rd = Getregindex(inst.m_tokens[1].m_value);
                string rs1 = Getregindex(inst.m_tokens[2].m_value);
                string funct = opcodes[inst.m_tokens[0].m_value];

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
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid shift amount `{shamt}`\n", 1);
                    mc = "000000"
                        + "00000"
                        + rs1
                        + rd
                        + shamt
                        + funct;
                }
                else
                {
                    // 11111 00000 00001 00000101010
                    // 11111 00000 00101 00000101010
                    string? rs2 = Getregindex(inst.m_tokens[3].m_value);
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
        string GetImmed(string immed)
        {
            // andi, ori, xori (they do zero extend)
            if ((immed.StartsWith("0x") || immed.StartsWith("0X")))
            {
                try
                {
                    short temp = Convert.ToInt16(immed, 16);
                    return Convert.ToString(temp, 2).PadLeft(16, '0');
                }
                catch
                {
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unable to convert to 16-bit signed immediate : `{immed}`\n", 1);
                    return "";
                }
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
                Shartilities.Log(Shartilities.LogType.ERROR, $"invalid immediate : `{immed}`\n", 1);
            return "";
        }
        string Getitypeinst(Instruction inst)
        {
            if (inst.m_tokens.Count > 0 && (inst.m_tokens[0].m_value == "sw" || inst.m_tokens[0].m_value == "lw") && inst.m_tokens.Count != 6)
                Shartilities.Log(Shartilities.LogType.ERROR, $"invlid load/store instruction\n", 1);
            else if (!(inst.m_tokens[0].m_value == "sw" || inst.m_tokens[0].m_value == "lw") && inst.m_tokens.Count != 4)
                Shartilities.Log(Shartilities.LogType.ERROR, $"invalid I-Type instruction\n", 1);
            string mc;
            string opcode = opcodes[inst.m_tokens[0].m_value];

            if (Isbranch(inst.m_tokens[0].m_value))
            {
                string reg1 = Getregindex(inst.m_tokens[1].m_value);
                string reg2 = Getregindex(inst.m_tokens[2].m_value);
                if (!labels.ContainsKey(inst.m_tokens[3].m_value))
                    Shartilities.Log(Shartilities.LogType.ERROR, $"label `{inst.m_tokens[3].m_value}` not found\n", 1);
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
                    Shartilities.Log(Shartilities.LogType.ERROR, $"invalid immed for branch instruction `{immed}`\n", 1);
                string rs1 = reg1;
                string rs2 = reg2;
                mc = opcode + rs1 + rs2 + immed;
            }
            else if (inst.m_tokens[0].m_value == "lw" || inst.m_tokens[0].m_value == "sw")
            { // sw $1, 0($1)
                string reg1 = Getregindex(inst.m_tokens[1].m_value);
                string reg2 = Getregindex(inst.m_tokens[4].m_value);
                string immed = GetImmed(inst.m_tokens[2].m_value);
                string rd = reg1;
                string rs1 = reg2;
                mc = opcode + rs1 + rd + immed;
            }
            else
            {
                string reg1 = Getregindex(inst.m_tokens[1].m_value);
                string reg2 = Getregindex(inst.m_tokens[2].m_value);
                string immed = GetImmed(inst.m_tokens[3].m_value);
                string rd = reg1;
                string rs1 = reg2;
                mc = opcode + rs1 + rd + immed;
            }

            return mc;
        }
        string Getjtypeinst(Instruction inst)
        {
            if (inst.m_tokens.Count != 2 || !labels.ContainsKey(inst.m_tokens[1].m_value))
            {
                Shartilities.Log(Shartilities.LogType.ERROR, $"invalid J-Type instruction\n", 1);
                return "";
            }
            int lbl = labels[inst.m_tokens[1].m_value];
            string immed = Convert.ToString(lbl, 2);
            immed = immed.PadLeft(26, '0');
            string mc = opcodes[inst.m_tokens[0].m_value] + immed;
            return mc;
        }
        List<string> GetMachineCodeOfProg(ref Program program)
        {
            List<string> mcs = [];
            curr_inst_index = 0;
            for (int i = 0; i < program.instructions.Count; i++)
            {
                InstructionType type = GetInstType(program.instructions[i].m_tokens[0].m_value);
                Instruction temp = program.instructions[i];
                temp.m_type = type;
                program.instructions[i] = temp;
                string mc = GetMcOfInst(program.instructions[i]);
                mcs.Add(mc);
                curr_inst_index++;
            }

            return mcs;
        }
        string GetMcOfInst(Instruction inst)
        {
            if (inst.m_tokens.Count > 0 && (inst.m_tokens[0].m_value == "hlt" || inst.m_tokens[0].m_value == "nop"))
                return opcodes[inst.m_tokens[0].m_value].PadRight(32, '0');
            // here we construct the binaries of a given instruction
            switch (inst.m_type)
            {
                case InstructionType.rtype:
                    return Getrtypeinst(inst);
                case InstructionType.itype:
                    return Getitypeinst(inst);
                case InstructionType.jtype:
                    return Getjtypeinst(inst);
                default:
                    Shartilities.UNREACHABLE($"GetMcOfInst");
                    return "";
            }
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
                    new([new("slt"), new("$at"), new($"{inst.m_tokens[1].m_value}"), new("$0") ]),
                    new([new(branch), new("$at"), new("$0"), new($"{inst.m_tokens[2].m_value}") ]),
                ];
            }
            else
            {
                Shartilities.Log(Shartilities.LogType.ERROR, $"invalid pseudo instruction `{inst.m_tokens[0].m_value}`\n", 1);
            }
            lblINVINST = true;

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
                else if (c == '+')
                {
                    consume();
                    string a = buffer.ToString();
                    buffer.Clear();
                    while (peek().HasValue && !peek('(').HasValue)
                    {
                        buffer.Append(consume());
                    }
                    string b = buffer.ToString();
                    buffer.Clear();
                    string sum = (Convert.ToInt32(a) + Convert.ToInt32(b)).ToString();
                    instruction.m_tokens.Add(new(sum));
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
        public Program? AssemblyProgram(List<string> in_prog)
        {
            lblINVINST = false;
            lblinvlabel = false;
            lblmultlabels = false;
            labels.Clear();

            in_prog.RemoveAll(line => string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line));
            m_prog = [.. in_prog];
            m_prog.Add("HLT");
            Program program = TokenizeProg(m_prog);
            SubstitutePseudoInProg(ref program);
            Subtitute_labels(ref program);
            List<string> mc = GetMachineCodeOfProg(ref program);
            program.mc = mc;
            return program;
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
                    instruction.m_tokens[^1] = new(LabelValue);
                }
                string inst = "";
                instruction.m_tokens.ForEach(token => inst += token.m_value + " ");
                ret.Add(inst);
            }
            return ret;
        }
    }
}
