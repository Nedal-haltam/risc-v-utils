using System.ComponentModel.Design;
using System.Globalization;
using System.Text;

public static class LibUtils
{
    public static readonly Dictionary<string, (string, int)> REG_LIST = new()
    {
        {"zero", new("00000", 0)},
        {"ra"  , new("00001", 1)},
        {"sp"  , new("00010", 2)},
        {"gp"  , new("00011", 3)},
        {"tp"  , new("00100", 4)},
        {"t0"  , new("00101", 5)},
        {"at"  , new("00101", 5)},
        {"t1"  , new("00110", 6)},
        {"t2"  , new("00111", 7)},
        {"s0"  , new("01000", 8)},
        {"s1"  , new("01001", 9)},
        {"a0"  , new("01010", 10)},
        {"a1"  , new("01011", 11)},
        {"a2"  , new("01100", 12)},
        {"a3"  , new("01101", 13)},
        {"a4"  , new("01110", 14)},
        {"a5"  , new("01111", 15)},
        {"a6"  , new("10000", 16)},
        {"a7"  , new("10001", 17)},
        {"s2"  , new("10010", 18)},
        {"s3"  , new("10011", 19)},
        {"s4"  , new("10100", 20)},
        {"s5"  , new("10101", 21)},
        {"s6"  , new("10110", 22)},
        {"s7"  , new("10111", 23)},
        {"s8"  , new("11000", 24)},
        {"s9"  , new("11001", 25)},
        {"s10" , new("11010", 26)},
        {"s11" , new("11011", 27)},
        {"t3"  , new("11100", 28)},
        {"t4"  , new("11101", 29)},
        {"t5"  , new("11110", 30)},
        {"t6"  , new("11111", 31)},
    };
    public readonly static Dictionary<string, InstInfo> Infos = new()
    {
        // R-Type
        {"add"   , new("0110011", "000", "0000000")},
        {"sub"   , new("0110011", "000", "0110000")},
        {"mul"   , new("0110011", "000", "0000001")},
        {"sll"   , new("0110011", "001", "0000000")},
        {"slt"   , new("0110011", "010", "0000000")},
        {"seq"   , new("0110011", "010", "0000001")},
        {"sne"   , new("0110011", "010", "0000010")},
        {"sltu"  , new("0110011", "011", "0000000")},
        {"xor"   , new("0110011", "100", "0000000")},
        {"div"   , new("0110011", "100", "0000001")},
        {"srl"   , new("0110011", "101", "0000000")},
        {"sra"   , new("0110011", "101", "0100000")},
        {"divu"  , new("0110011", "101", "0000001")},
        {"or"    , new("0110011", "110", "0000000")},
        {"rem"   , new("0110011", "110", "0000001")},
        {"and"   , new("0110011", "111", "0000000")},
        {"remu"  , new("0110011", "111", "0000001")},
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
        {"ld"    , new("0000011", "011", "")},
        {"lbu"   , new("0000011", "100", "")},
        {"lhu"   , new("0000011", "101", "")},
        {"jalr"  , new("1110111", "000", "")},
        // S-Type
        {"sb"    , new("0100011", "000", "")},
        {"sh"    , new("0100011", "001", "")},
        {"sw"    , new("0100011", "010", "")},
        {"sd"    , new("0100011", "011", "")},
        {"beq"   , new("1100011", "000", "")},
        {"bne"   , new("1100011", "001", "")},
        {"blt"   , new("1100011", "100", "")},
        {"bge"   , new("1100011", "101", "")},
        {"bltu"  , new("1100011", "110", "")},
        {"bgeu"  , new("1100011", "111", "")},
        // U-Type
        {"lui"   , new("0110111", "", "")},
        {"auipc" , new("0010111", "", "")},
        {"jal"   , new("1111111", "", "")},
        {"addi20u" , new("1111110", "", "")},
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
    public static string StringToBin(string imm) => LongToBin(StringToLong(imm));
    public static long StringToLong(string imm)
    {
        if (imm.StartsWith("0x"))
        {
            return Convert.ToInt64(imm, 16);
        }
        else if (imm.StartsWith("0b"))
        {
            return Convert.ToInt64(imm[2..], 2);
        }
        else
        {
            if (ulong.TryParse(imm, out ulong unsigned))
                return (long)unsigned;
            else if (long.TryParse(imm, out long signed))
                return signed;
            else
                Shartilities.Log(Shartilities.LogType.ERROR, $"could not prase immediate: {imm}\n", 1);
            Shartilities.UNREACHABLE("StringToLong");
            return 0;
        }
    }
    public static string LongToBin(long NumberLiteral) => zext(Convert.ToString(NumberLiteral, 2), 64);
    public static string sext(string imm, int length) => imm.PadLeft(length, imm[0]);
    public static string zext(string imm, int length) => imm.PadLeft(length, '0');
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
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported data memory directive {Directive}\n", 1);
                }
                Directive = DataMemoryValues[i];
                if (Directive == ".space")
                {
                    i++;
                    if (!uint.TryParse(DataMemoryValues[i], out uint count))
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid immediate {DataMemoryValues[i]}\n", 1);
                    for (int j = 0; j < count; j++) DM_Bytes.Add("0");
                }
                else if (Directive == ".string")
                {
                    i++;
                    if (!uint.TryParse(DataMemoryValues[i], out uint len))
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid immediate {DataMemoryValues[i]}\n", 1);
                    i++;
                    if (i + len > DataMemoryValues.Count)
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid length of string literal {len}\n", 1);
                    for (int j = i; j < i + len; j++)
                    {
                        if (DataMemoryValues[i].Length > 0)
                            DM_Bytes.Add(((byte)DataMemoryValues[j][0]).ToString());
                        else
                            Shartilities.Log(Shartilities.LogType.ERROR, $"no character was provided in string literal\n", 1);
                    }
                    i += (int)len - 1;
                }
                else if (Directive == ".word")
                {
                    i++;
                    if (!uint.TryParse(DataMemoryValues[i], out uint count))
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid immediate {DataMemoryValues[i]}\n", 1);
                    i++;
                    if (i + count > DataMemoryValues.Count)
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid number of words {count}\n", 1);
                    for (int j = i; j < i + count; j++)
                    {
                        if (DataMemoryValues[i].Length > 0)
                        {
                            string value = DataMemoryValues[j];
                            string bin = "";
                            if (value.StartsWith("0x"))
                                bin = zext(Convert.ToString(Convert.ToUInt32(value, 16), 2), 32);
                            else if (uint.TryParse(value, out uint signed))
                                bin = zext(Convert.ToString(signed, 2), 32);
                            else if (int.TryParse(value, out int unsigned))
                                bin = zext(Convert.ToString(unsigned, 2), 32);
                            else
                                Shartilities.Log(Shartilities.LogType.ERROR, $"invalid value in directive word {value}\n", 1);
                            for (int k = 0; k < 4; k++)
                                DM_Bytes.Add(Convert.ToByte(GetFromIndexLittle(bin, 8 * (k + 1) - 1, 8 * k), 2).ToString());
                        }
                        else
                            Shartilities.Log(Shartilities.LogType.ERROR, $"no character was provided in string literal\n", 1);
                    }
                    i += (int)count - 1;
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
        foreach (string reg in regs)
        {
            long signed = Convert.ToInt64(reg, 2);
            ulong unsigned = Convert.ToUInt64(reg, 2);
            sb.Append($"index = {i++,10} , reg_out : signed = {signed,10} , unsigned = {unsigned,10}\n");
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
            sbyte signed = Convert.ToSByte(mem, 2);
            byte unsigned = Convert.ToByte(mem, 2);
            sb.Append($"Mem[{i++,4}] = signed = {signed,3} , unsigned = {unsigned,3}\n");
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
            Shartilities.Log(Shartilities.LogType.ERROR, $"could not index {str}\n", 1);
        }
        Shartilities.UNREACHABLE("GetFromIndexLittle");
        return "";
    }
}
