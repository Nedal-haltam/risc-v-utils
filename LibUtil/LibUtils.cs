using System.Globalization;
using System.Text;

public static class LibUtils
{
    public struct Instruction
    {
        public List<string> m_tokens;
        public int m_line;
        public Instruction()
        {
            m_line = -1;
            m_tokens = [];
        }
        public Instruction(List<string> tokens, int line)
        {
            m_tokens = tokens;
            m_line = line;
        }
    }
    public struct Program
    {
        public List<Instruction> Instructions;
        public List<string> DataMemoryValues;
        public List<string> MachineCodes;
        public Program()
        {
            Instructions = [];
            MachineCodes = [];
            DataMemoryValues = [];
        }
    }
    public struct InstInfo(string Opcode, string Funct3, string Funct7)
    {
        public string Opcode = Opcode;
        public string Funct3 = Funct3;
        public string Funct7 = Funct7;
    }
    // make it into two separate functions
    // 1- string imm -> number literal
    // 2- number literal -> binary string
    public static string GetImmediateToBin(string imm)
    {
        if (imm.StartsWith("0x"))
        {
            imm = imm[2..];
            if (UInt32.TryParse(imm, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out UInt32 value))
                return Convert.ToString(value, 2).PadLeft(32, '0');
            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
        }
        else
        {
            if (UInt32.TryParse(imm, out UInt32 UnsignedValue))
                return Convert.ToString(UnsignedValue, 2).PadLeft(32, '0');
            else if (Int32.TryParse(imm, out Int32 SignedValue))
                return Convert.ToString(SignedValue, 2).PadLeft(32, '0');
            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
        }
        Shartilities.UNREACHABLE("GetImmediate");
        return "BogusAmogus";
    }
    public static List<string> GetIM_INIT(List<string> MachinceCodes, List<Instruction> Instructions)
    {
        Shartilities.TODO("GetIM_INIT is not implemented");
        return [];
    }
    public static List<string> GetDM_INIT(List<string> DataMemoryValues)
    {
        Shartilities.TODO("GetDM_INIT is not implemented");
        return [];
    }
    public static List<string> GetIM(List<string> MachinceCodes)
    {
        return MachinceCodes;
    }
    static bool IsValidDataMemoryDirective(string Directive)
    {
        return Directive == ".space" || Directive == ".string" || Directive == ".word";
    }
    public static List<string> GetDM(List<string> DataMemoryValues)
    {
        List<string> DM_Bytes = [];
        string? Directive = null;
        for (int i = 0; i < DataMemoryValues.Count; i++)
        {
            if (DataMemoryValues[i].StartsWith('.'))
            {
                if (!IsValidDataMemoryDirective(DataMemoryValues[i]))
                {
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported data memory directive `{Directive}`\n", 1);
                }
                Directive = DataMemoryValues[i];
                if (Directive == ".space")
                {
                    i++;
                    if (!uint.TryParse(DataMemoryValues[i], out uint count))
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid immediate `{DataMemoryValues[i]}`\n", 1);
                    for (int j = 0; j < count; j++) DM_Bytes.Add("0");
                }
                else if (Directive == ".string")
                {
                    i++;
                    if (!uint.TryParse(DataMemoryValues[i], out uint len))
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid immediate `{DataMemoryValues[i]}`\n", 1);
                    i++;
                    if (i + len > DataMemoryValues.Count)
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid length of string literal `{len}`\n", 1);
                    for (int j = i; j < i + len; j++)
                    {
                        if (DataMemoryValues[i].Length > 0)
                            DM_Bytes.Add(((byte)DataMemoryValues[j][0]).ToString());
                        else
                            Shartilities.Log(Shartilities.LogType.ERROR, $"no character was provided in string literal\n", 1);
                    }
                    i += (int)len;
                }
                else if (Directive == ".word")
                {
                    Shartilities.TODO(".word parsing");
                }
            }
            else
            {
                Shartilities.Log(Shartilities.LogType.ERROR, $"error in iterating Data memory values\n", 1);
            }
        }
        return DM_Bytes;
    }
    public static void print_regs(List<string> regs)
    {
        Console.Write(get_regs(regs).ToString());
    }
    public static void print_DM(List<string> DM)
    {
        Console.Write(GetDM(DM).ToString());
    }
    public static StringBuilder get_regs(List<string> regs)
    {
        StringBuilder sb = new();
        sb.Append("Register file content : \n");
        int i = 0;
        foreach (string n in regs)
        {
            Int32 signed = Int32.Parse(n);
            UInt32 unsigned = UInt32.Parse(n);
            string temp = $"index = {i++,10} , reg_out : signed = {signed,10} , unsigned = {unsigned,10}\n";
            sb.Append(temp);
        }
        return sb;
    }
    public static StringBuilder get_DM(List<string> DM)
    {
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
    public static string GetFromIndexLittle(string NameofStringVariable, string str, int to, int from)
    {
        int len = to - from + 1;
        try
        {
            return str.Substring(str.Length - to - 1, len);
        }
        catch
        {
            Shartilities.Log(Shartilities.LogType.ERROR, $"could not index `{NameofStringVariable}`\n", 1);
        }
        Shartilities.UNREACHABLE("GetFromIndexLittle");
        return "";
    }
}
