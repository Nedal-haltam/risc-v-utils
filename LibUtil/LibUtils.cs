using System.Globalization;
using System.Text;

public static class LibUtils
{
    public static readonly Dictionary<string, int> REG_LIST = new()
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
    public readonly static Dictionary<string, InstInfo> Infos = new()
    {
        // R-Type
        {"add"   , new("0110011", "000", "0000000")},
        {"sub"   , new("0110011", "000", "0110000")},
        {"sll"   , new("0110011", "001", "0000000")},
        {"slt"   , new("0110011", "010", "0000000")},
        {"sltu"  , new("0110011", "011", "0000000")},
        {"xor"   , new("0110011", "100", "0000000")},
        {"srl"   , new("0110011", "101", "0000000")},
        {"sra"   , new("0110011", "101", "0100000")},
        {"or"    , new("0110011", "110", "0000000")},
        {"and"   , new("0110011", "111", "0000000")},
        // I-Type
        {"addi"  , new("0010011", "000", "")},
        {"slti"  , new("0010011", "010", "")},
        {"sltiu" , new("0010011", "011", "")},
        {"xori"  , new("0010011", "100", "")},
        {"ori"   , new("0010011", "110", "")},
        {"andi"  , new("0010011", "111", "")},
        {"slli"  , new("0010011", "001", "")},
        {"srli"  , new("0010011", "101", "")},
        {"srai"  , new("0010011", "101", "")},
        {"ecall" , new("1110011", "000", "")},
        {"lb"    , new("0000011", "000", "")},
        {"lh"    , new("0000011", "001", "")},
        {"lw"    , new("0000011", "010", "")},
        {"lbu"   , new("0000011", "100", "")},
        {"lhu"   , new("0000011", "101", "")},
        {"jalr"  , new("1110111", "000", "")},
        // S-Type
        {"sb"    , new("0100011", "000", "")},
        {"sh"    , new("0100011", "001", "")},
        {"sw"    , new("0100011", "010", "")},
        {"beq"   , new("1100011", "000", "")},
        {"bne"   , new("1100011", "001", "")},
        {"blt"   , new("1100011", "100", "")},
        {"bge"   , new("1100011", "101", "")},
        // U-Type
        {"lui"   , new("0110111", "", "")},
        {"auipc" , new("0010111", "", "")},
        {"jal"   , new("1111111", "", "")},
    };

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
    // TODO:
    //      - test the edge case of load immediate `li` and go back to the assembler
    //      - test all instruction in all cases (specially ones that uses immediate)
    //      - test shift instructions and ensure that it uses only 5-bits of the oprand
    public static dynamic StringToNumberLiteral(string imm)
    {
        if (imm.StartsWith("0x"))
        {
            imm = imm[2..];
            if (UInt32.TryParse(imm, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out UInt32 value))
                return value;
            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
        }
        else
        {
            if (UInt32.TryParse(imm, out UInt32 UnsignedValue))
                return UnsignedValue;
            else if (Int32.TryParse(imm, out Int32 SignedValue))
                return SignedValue;
            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
        }
        Shartilities.UNREACHABLE("GetImmediate");
        return "BogusAmogus";
    }
    public static string NumberLiteralToBinaryString(dynamic NumberLiteral)
    {
        return Convert.ToString(NumberLiteral, 2).PadLeft(32, '0');
    }
    public static string GetImmediateToBin(string imm)
    {
        return NumberLiteralToBinaryString(StringToNumberLiteral(imm));
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
    public static List<string> ParseDataMemoryValues(List<string> DataMemoryValues)
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
    public static void PrintRegs(List<string> regs)
    {
        Console.Write(GetRegs(regs).ToString());
    }
    public static void PrintDM(List<string> DM)
    {
        Console.Write(GetDM(DM).ToString());
    }
    public static StringBuilder GetRegs(List<string> regs)
    {
        StringBuilder sb = new();
        sb.Append("Register file content : \n");
        int i = 0;
        foreach (string n in regs)
        {
            Int32 signed = Int32.Parse(n);
            UInt32 unsigned = signed < 0 ? (uint)signed : UInt32.Parse(n);
            string temp = $"index = {i++,10} , reg_out : signed = {signed,10} , unsigned = {unsigned,10}\n";
            sb.Append(temp);
        }
        return sb;
    }
    public static StringBuilder GetDM(List<string> DM)
    {
        StringBuilder sb = new();
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
    public static string GetFromIndexLittle(string str, int to, int from)
    {
        int len = to - from + 1;
        try
        {
            return str.Substring(str.Length - to - 1, len);
        }
        catch
        {
            Shartilities.Log(Shartilities.LogType.ERROR, $"could not index `{str}`\n", 1);
        }
        Shartilities.UNREACHABLE("GetFromIndexLittle");
        return "";
    }
}
