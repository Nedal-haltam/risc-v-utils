
using System.Diagnostics;
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
        string GetMcOfInst(Instruction inst)
        {
            // we are going to support up to 3.8, not all of them but upto 3.8
            // document each instruction it's format and any neccessary/useful/important information
            // 
            Shartilities.TODO($"assemble here, my friend");
            List<Token> tokens = inst.m_tokens;
            string mnem = tokens[0].m_value;
            switch (mnem)
            {
                default:
                    Shartilities.Log(Shartilities.LogType.ERROR, $"invalid instruction mnemonic `{mnem}`\n", 1);
                    return "";
            }
        }
        List<string> GetMachineCodeOfProg(ref Program program)
        {
            List<string> mcs = [];
            curr_inst_index = 0;
            for (int i = 0; i < program.instructions.Count; i++)
            {
                mcs.Add(GetMcOfInst(program.instructions[i]));
                curr_inst_index++;
            }
            return mcs;
        }
        static int LineNumber([System.Runtime.CompilerServices.CallerLineNumber] int LineNumber = 0)
        {
            return LineNumber;
        }
        static string FilePath([System.Runtime.CompilerServices.CallerFilePath] string FilePath = "")
        {
            return FilePath;
        }
        bool IsPseudo(string inst)
        {
            return inst == "bltz" ||
                   inst == "bgez" ||
                   inst == "li" ||
                   inst == "la" ||
                   inst == "call" ||
                   inst == "ret" ||
                   inst == "mv";
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
        void SubstitutePseudoInProg(ref Program program)
        {
            for (int i = 0; i < program.instructions.Count; i++)
            {
                if (IsPseudo(program.instructions[i].m_tokens[0].m_value))
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
        public Program AssembleProgram(List<string> in_prog)
        {
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
    }
}
