using System.Runtime.InteropServices;
using static System.Text.RegularExpressions.Regex;

namespace Assembler
{
    public struct Token(string value)
    {
        public string value = value;
    }
    public struct Instruction
    {
        public List<Token> m_tokens;
        public int m_line;
        public Instruction()
        {
            m_line = -1;
            m_tokens = [];
        }
        public Instruction(List<Token> tokens, int line)
        {
            m_tokens = tokens;
            m_line = line;
        }
    }
    public struct Program
    {
        public List<string> MachineCodes;
        public List<Instruction> Instructions;
        public List<string> DataMemoryValues;
        public Program()
        {
            Instructions = [];
            MachineCodes = [];
            DataMemoryValues = [];
        }
    }

    public static class Assembler
    {
        static readonly Dictionary<string, int> REG_LIST = new()
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
        struct InstInfo(string Opcode, string Funct3, string Funct7)
        {
            public string Opcode = Opcode;
            public string Funct3 = Funct3;
            public string Funct7 = Funct7;
        }
        static class INSTRUCTIONS
        {
            readonly static Dictionary<string, InstInfo> Infos = new()
            {
                // U-Type
                {"lui"   , new("0110111", "", "")},
                {"auipc" , new("0010111", "", "")},
                {"jal"   , new("1111111", "", "")},
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
            };
            public static string GetRtypeInst(string mnem, string rs1, string rs2, string rd)
            {
                Shartilities.Assert(rs1.Length == 5 && rs2.Length == 5 && rd.Length == 5, $"invalid format in instruction `{mnem}`, lengths are: rs1={rs1.Length}, rs2={rs2.Length}, rd={rd.Length}");
                if (!Infos.ContainsKey(mnem))
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
                InstInfo info = Infos[mnem];
                return info.Funct7 + rs2 + rs1 + info.Funct3 + rd + info.Opcode;
            }
            public static string GetItypeInst(string mnem, string imm12, string rs1, string rd)
            {
                Shartilities.Assert(imm12.Length == 12 && rs1.Length == 5 && rd.Length == 5, $"invalid format in instruction `{mnem}`, lengths are: imm12={imm12.Length}, rs1={rs1.Length}, rd={rd.Length}");
                if (!Infos.ContainsKey(mnem))
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
                InstInfo info = Infos[mnem];
                return imm12 + rs1 + info.Funct3 + rd + info.Opcode;
            }
            public static string GetStypeInst(string mnem, string imm12, string rs1, string rs2)
            {
                Shartilities.Assert(imm12.Length == 12 && rs1.Length == 5 && rs2.Length == 5, $"invalid format in instruction `{mnem}`, lengths are: imm12={imm12.Length}, rs1={rs1.Length}, rs2={rs2.Length}");
                if (!Infos.ContainsKey(mnem))
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
                InstInfo info = Infos[mnem];
                return imm12[..7] + rs2 + rs1 + info.Funct3 + imm12.Substring(7, 5) + info.Opcode;
            }
            public static string GetUtypeInst(string mnem, string imm20, string rd)
            {
                Shartilities.Assert(imm20.Length == 20 && rd.Length == 5, $"invalid format in instruction `{mnem}`, lengths are: imm20={imm20.Length}, rd={rd.Length}");
                if (!Infos.ContainsKey(mnem))
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
                return imm20 + rd + Infos[mnem].Opcode;
            }
        }
        static string GetRegisterIndex(string reg)
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
        static void Check(string mnem, int have, int want) => Shartilities.Assert(have == want, $"invalid `{mnem}` instruction");
        static List<string> Instruction2MachineCodes(Instruction inst)
        {
            // references:
            // - https://msyksphinz-self.github.io/riscv-isadoc/
            // - https://riscv.github.io/riscv-isa-manual/snapshot/unprivileged/
            Shartilities.TODO($"assemble here, my friend");
            List<Token> ts = inst.m_tokens;
            string mnem = ts[0].value;
            // TODO: support negative numbers
            switch (mnem)
            {
                case "lui":
                    {
                        // lui rd,imm
                        // x[rd] = SignExtended(immediate[31:12] << 12)
                        Check(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1].value);
                        string imm = ts[2].value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0')[..20];
                        return [INSTRUCTIONS.GetUtypeInst(mnem, imm, rd)];
                    }
                case "auipc":
                    {
                        // auipc rd,imm
                        // x[rd] = pc + SignExtended(immediate[31:12] << 12)
                        Check(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1].value);
                        string imm = ts[2].value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0')[..20];
                        return [INSTRUCTIONS.GetUtypeInst(mnem, imm, rd)];
                    }
                case "addi":
                    {
                        // addi rd,rs1,imm
                        // x[rd] = x[rs1] + SignExtended(immediate)
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string imm = ts[3].value;
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
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rd = GetRegisterIndex(ts[1].value);
                        return [INSTRUCTIONS.GetItypeInst("addi", "0".PadLeft(12, '0'), rs1, rd)];
                    }
                case "slti":
                    {
                        // slti rd,rs1,imm
                        // x[rd] = x[rs1] <s SignExtended(immediate)
                        // x[rd] = (signed(x[rs1]) < signed(SignExtended(immediate))) ? 1 : 0
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string imm = ts[3].value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "sltiu":
                    {
                        // sltiu rd,rs1,imm
                        // x[rd] = x[rs1] <u SignExtended(immediate)
                        // the difference is that the numbers are treated as unsigned instead
                        // x[rd] = (unsigned(x[rs1]) < unsigned(ZeroExtended(immediate))) ? 1 : 0
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string imm = ts[3].value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "xori":
                    {
                        // xori rd,rs1,imm
                        // x[rd] = x[rs1] ^ SignExtended(immediate)
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string imm = ts[3].value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "not":
                    {
                        // not rd,rs1
                        // x[rd] = x[rs1] ^ SignExtended(-1)
                        Check(mnem, ts.Count, 3);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rd = GetRegisterIndex(ts[1].value);
                        return [INSTRUCTIONS.GetItypeInst("xori", "1".PadLeft(12, '1'), rs1, rd)];
                    }
                case "ori":
                    {
                        // ori rd,rs1,imm
                        // x[rd] = x[rs1] | SignExtended(immediate)
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string imm = ts[3].value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "andi":
                    {
                        // andi rd,rs1,imm
                        // x[rd] = x[rs1] & SignExtended(immediate)
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string imm = ts[3].value;
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
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string imm = ts[3].value;
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
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string imm = ts[3].value;
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
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string imm = ts[3].value;
                        if (!UInt32.TryParse(imm, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{imm}`\n", 1);
                        imm = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 4, 5).PadLeft(12, '0');
                        imm = string.Concat(imm.AsSpan()[..1], "1", imm.AsSpan(2));
                        return [INSTRUCTIONS.GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "add":
                    {
                        // add rd,rs1,rs2
                        // x[rd] = x[rs1] + x[rs2]
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rs2 = GetRegisterIndex(ts[3].value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];

                    }
                case "sub":
                    {
                        // sub rd,rs1,rs2
                        // x[rd] = x[rs1] - x[rs2]
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rs2 = GetRegisterIndex(ts[3].value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "sll":
                    {
                        // sll rd,rs1,rs2
                        // x[rd] = x[rs1] << x[rs2]
                        // NOTE: Performs logical left shift on the value in register rs1 by the shift amount held in the
                        // ```lower 5 bits of register rs2```
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rs2 = GetRegisterIndex(ts[3].value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "slt":
                    {
                        // slt rd,rs1,rs2
                        // x[rd] = x[rs1] <s x[rs2]
                        // x[rd] = (signed(x[rs1]) < signed(x[rs2])) ? 1 : 0
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rs2 = GetRegisterIndex(ts[3].value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "sltu":
                    {
                        // sltu rd,rs1,rs2
                        // x[rd] = x[rs1] <u x[rs2]
                        // x[rd] = (unsigned(x[rs1]) < unsigned(x[rs2])) ? 1 : 0
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rs2 = GetRegisterIndex(ts[3].value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "xor":
                    {
                        // xor rd,rs1,rs2
                        // x[rd] = x[rs1] ^ x[rs2]
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rs2 = GetRegisterIndex(ts[3].value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "srl":
                    {
                        // srl rd,rs1,rs2
                        // x[rd] = x[rs1] >>u x[rs2]
                        // NOTE: Logical right shift on the value in register rs1 by the shift amount held in the
                        // ```lower 5 bits of register rs2```
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rs2 = GetRegisterIndex(ts[3].value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "sra":
                    {
                        // sra rd,rs1,rs2
                        // x[rd] = x[rs1] >>s x[rs2]
                        // NOTE: Performs arithmetic right shift on the value in register rs1 by the shift amount held in the
                        // ```lower 5 bits of register rs2```
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rs2 = GetRegisterIndex(ts[3].value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "or":
                    {
                        // or rd,rs1,rs2
                        // x[rd] = x[rs1] | x[rs2]
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rs2 = GetRegisterIndex(ts[3].value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "and":
                    {
                        // and rd,rs1,rs2
                        // x[rd] = x[rs1] & x[rs2]
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string rs1 = GetRegisterIndex(ts[2].value);
                        string rs2 = GetRegisterIndex(ts[3].value);
                        return [INSTRUCTIONS.GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "ecall":
                    {
                        // ecall
                        // RaiseException(EnvironmentCall)
                        return [INSTRUCTIONS.GetItypeInst(mnem, "0".PadLeft(12, '0'), "00000", "00000")];
                    }
                case "lb":
                    {
                        // lb rd,offset(rs1)
                        // x[rd] = SignExtended(M[x[rs1] + SignExtended(offset)][7:0])
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string offset = ts[2].value;
                        string rs1 = GetRegisterIndex(ts[3].value);
                        if (!UInt32.TryParse(offset, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{offset}`\n", 1);
                        offset = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "lh":
                    {
                        // lh rd,offset(rs1)
                        // x[rd] = SignExtended(M[x[rs1] + SignExtended(offset)][15:0])
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string offset = ts[2].value;
                        string rs1 = GetRegisterIndex(ts[3].value);
                        if (!UInt32.TryParse(offset, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{offset}`\n", 1);
                        offset = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "lw":
                    {
                        // lw rd,offset(rs1)
                        // x[rd] = SignExtended(M[x[rs1] + SignExtended(offset)][31:0])
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string offset = ts[2].value;
                        string rs1 = GetRegisterIndex(ts[3].value);
                        if (!UInt32.TryParse(offset, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{offset}`\n", 1);
                        offset = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "lbu":
                    {
                        // lbu rd,offset(rs1)
                        // x[rd] = ZeroExtended(M[x[rs1] + SignExtended(offset)][7:0])
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string offset = ts[2].value;
                        string rs1 = GetRegisterIndex(ts[3].value);
                        if (!UInt32.TryParse(offset, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{offset}`\n", 1);
                        offset = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "lhu":
                    {
                        // lhu rd,offset(rs1)
                        // x[rd] = ZeroExtended(M[x[rs1] + SignExtended(offset)][15:0])
                        Check(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1].value);
                        string offset = ts[2].value;
                        string rs1 = GetRegisterIndex(ts[3].value);
                        if (!UInt32.TryParse(offset, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{offset}`\n", 1);
                        offset = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "sb":
                    {
                        // sb rs2,offset(rs1)
                        // M[x[rs1] + SignExtended(offset)] = x[rs2][7:0]
                        // NOTE: Store 8-bit, values from the
                        // ```low bits of register rs2 to memory.```
                        string rs2 = GetRegisterIndex(ts[1].value);
                        string offset = ts[2].value;
                        string rs1 = GetRegisterIndex(ts[3].value);
                        if (!UInt32.TryParse(offset, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{offset}`\n", 1);
                        offset = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetStypeInst(mnem, offset, rs1, rs2)];
                    }
                case "sh":
                    {
                        // sh rs2,offset(rs1)
                        // M[x[rs1] + SignExtended(offset)] = x[rs2][15:0]
                        // NOTE: Store 16-bit, values from the
                        // ```low bits of register rs2 to memory.```
                        string rs2 = GetRegisterIndex(ts[1].value);
                        string offset = ts[2].value;
                        string rs1 = GetRegisterIndex(ts[3].value);
                        if (!UInt32.TryParse(offset, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{offset}`\n", 1);
                        offset = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetStypeInst(mnem, offset, rs1, rs2)];
                    }
                case "sw":
                    {
                        // sw rs2,offset(rs1)
                        // M[x[rs1] + SignExtended(offset)] = x[rs2][31:0]
                        string rs2 = GetRegisterIndex(ts[1].value);
                        string offset = ts[2].value;
                        string rs1 = GetRegisterIndex(ts[3].value);
                        if (!UInt32.TryParse(offset, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{offset}`\n", 1);
                        offset = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetStypeInst(mnem, offset, rs1, rs2)];
                    }
                case "jal":
                    {
                        // jal rd,offset
                        // x[rd] = pc+4; pc += SignExtended(offset) // this is an offset which is added to the pc not the final address
                        string rd = GetRegisterIndex(ts[1].value);
                        string offset = ts[2].value;
                        if (!UInt32.TryParse(offset, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{offset}`\n", 1);
                        offset = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 20, 20); // offset = offset[20:1]
                        // TODO: what is this?? -> [20|10:1|11|19:12] in the immediate index for the jal instruction format
                        //offset = string.Concat(offset[0], offset[10..20], offset[9], offset[1..9]); // offset = offset[20|10:1|11|19:12]
                        return [INSTRUCTIONS.GetUtypeInst(mnem, offset, rd)];
                    }
                case "jalr":
                    {
                        // jalr rd,rs1,offset
                        // t = pc+4;
                        // pc = (x[rs1] + SignExtended(offset)) & ~1;
                        // x[rd] = t
                        // NOTE: the steps above are important and should be implemented exactly as shown
                        // ofcourse they are gonna be executed in parallel in hardware but should be taken into account when performing the operation
                        string rd = ts[1].value;
                        string rs1 = ts[2].value;
                        string offset = ts[3].value;
                        if (!UInt32.TryParse(offset, out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"could not parse immediate `{offset}`\n", 1);
                        offset = Convert.ToString(value, 2).PadLeft(32, '0').Substring(31 - 11, 12);
                        return [INSTRUCTIONS.GetItypeInst(mnem, offset, rs1, rd)];
                    }
                default:
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid instruction mnemonic `{mnem}`\n", 1);
                        return [];
                    }
            }
        }
        //static List<Instruction> GetPseudo(Instruction inst)
        //{
        //    string mnem = inst.m_tokens[0].value;
        //    if ((mnem == "bltz" || mnem == "bgez") && inst.m_tokens.Count == 3)
        //    {
        //        string branch = "";
        //        if (mnem == "bltz")
        //        {
        //            branch = "bne";
        //        }
        //        else if (mnem == "bgez")
        //        {
        //            branch = "beq";
        //        }
        //        return [
        //            new([new("slt"), new("$at"), new($"{inst.m_tokens[1].value}"), new("$0") ]),
        //            new([new(branch), new("$at"), new("$0"), new($"{inst.m_tokens[2].value}") ]),
        //        ];
        //    }
        //    else if (mnem == "li" && inst.m_tokens.Count == 3)
        //    {
        //        return [
        //            new([new("ori"), new(inst.m_tokens[1].value), new("zero"), new(inst.m_tokens[2].value)])
        //        ];
        //    }
        //    else if (mnem == "la" && inst.m_tokens.Count == 3)
        //    {
        //        return [
        //            new([new("ori"), new(inst.m_tokens[1].value), new("zero"), new(inst.m_tokens[2].value)])
        //        ];
        //    }
        //    else if (mnem == "call")
        //    {
        //        return [
        //            new([new("jal"), new(inst.m_tokens[1].value)])
        //        ];
        //    }
        //    else if (mnem == "mv")
        //    {
        //        return [
        //            new([new("or"), new(inst.m_tokens[1].value), new(inst.m_tokens[2].value), new("zero")])
        //        ];
        //    }
        //    else if (mnem == "ret")
        //    {
        //        return [
        //            new([new("jr"), new("ra")])
        //        ];
        //    }
        //    else
        //    {
        //        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid pseudo instruction `{inst.m_tokens[0].value}`\n", 1);
        //        return [];
        //    }
        //}
        //public static List<string> GetInstsAsText(Assembler.Program program)
        //{
        //    List<string> ret = [];
        //    for (int i = 0; i < program.Instructions.Count; i++)
        //    {
        //        Assembler.Instruction instruction = program.Instructions[i];
        //        string mnem = instruction.m_tokens[0].value;
        //        if (mnem == "beq" || mnem == "bne")
        //        {
        //            string LabelValue = Convert.ToInt16(program.MachineCodes[i].Substring(16), 2).ToString();
        //            instruction.m_tokens[^1] = new(LabelValue);
        //        }
        //        string inst = "";
        //        instruction.m_tokens.ForEach(token => inst += token.value + " ");
        //        ret.Add(inst);
        //    }
        //    return ret;
        //}
        //public static (List<string>, List<string>, List<KeyValuePair<string, int>>) assemble_data_dir(List<string> data_dir)
        //{
        //    List<string> data = [];
        //    List<KeyValuePair<string, int>> addresses = [];
        //    int address = 0;
        //    for (int i = 0; i < data_dir.Count; i++)
        //    {
        //        if (data_dir[i].IndexOf(":") == -1)
        //            continue;
        //        string name = data_dir[i].Substring(0, data_dir[i].IndexOf(":"));
        //        if (data_dir[i].Substring(data_dir[i].IndexOf(":") + 1).IndexOf(":") == -1)
        //        {
        //            name = name.Trim();
        //            int addr = address;
        //            address += data_dir[i].Count(s => s == ',') + 1;
        //            addresses.Add(new KeyValuePair<string, int>(name, addr));
        //        }
        //        else
        //        {
        //            name = name.Trim();
        //            string temp = data_dir[i];
        //            string[] datas = data_dir[i].Substring(temp.IndexOf(":")).Replace(".word", "").Split(':');
        //            int addr = address;
        //            address += Convert.ToInt32(datas[2]) - Convert.ToInt32(datas[1]) + 1;
        //            addresses.Add(new KeyValuePair<string, int>(name, addr));
        //        }
        //    }
        //    for (int i = 0; i < data_dir.Count; i++)
        //    {
        //        int index = data_dir[i].IndexOf(':');
        //        if (index != -1)
        //        {
        //            string line = data_dir[i].Substring(index + 1);
        //            line = line.Trim();
        //            line = line.Replace(".word", "");
        //            if (line.Contains(':'))
        //            {
        //                List<string> vals = line.Split(':').ToList();
        //                int count = Convert.ToInt32(vals[1]) - Convert.ToInt32(vals[0]) + 1;
        //                for (int j = 0; j < count; j++)
        //                {
        //                    data.Add("0");
        //                }
        //            }
        //            else
        //            {
        //                List<string> vals = line.Split(',').ToList();
        //                foreach (string val in vals)
        //                {
        //                    int number = 0;
        //                    string snum = val.ToLower().Trim();
        //                    try
        //                    {
        //                        if (snum.StartsWith("0x"))
        //                            number = Convert.ToInt32(snum, 16);
        //                        else
        //                            number = Convert.ToInt32(snum);
        //                    }
        //                    catch (Exception)
        //                    {
        //                        number = 0;
        //                        //throw new Exception("invalid number");
        //                    }
        //                    data.Add(number.ToString());
        //                }
        //            }
        //        }
        //    }
        //    List<string> DM_INIT = [];
        //    List<string> DM_vals = [];
        //    for (int i = 0; i < data.Count; i++)
        //    {
        //        DM_vals.Add(data[i].ToString());
        //        if (data[i][0] == '-')
        //        {
        //            string temp = $"DataMem[{i,2}] <= -32'd{data[i].Substring(1)};";
        //            DM_INIT.Add(temp);
        //        }
        //        else
        //        {
        //            string temp = $"DataMem[{i,2}] <= 32'd{data[i]};";
        //            DM_INIT.Add(temp);
        //        }
        //    }

        //    return (DM_INIT, DM_vals, addresses);
        //}
        static (List<string>, List<string>) Get_directives(List<string> src)
        {
            for (int i = 0; i < src.Count; i++)
                src[i] = src[i].Trim();
            src.RemoveAll(x => string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x));

            int data_index = src.IndexOf(".section .data");
            int text_index = src.IndexOf(".section .text");

            List<string> curr_data_dir = [];
            List<string> curr_text_dir = [];

            if (text_index == -1)
                Shartilities.Log(Shartilities.LogType.ERROR, $"text directive doesn't exist\n", 1);

            if (data_index != -1)
            {
                int count = (int)MathF.Abs(text_index - data_index);
                if (data_index > text_index)
                {
                    curr_data_dir = src.GetRange(data_index, src.Count - count);
                    curr_text_dir = src.GetRange(text_index, count);
                }
                else
                {
                    curr_data_dir = src.GetRange(data_index, count);
                    curr_text_dir = src.GetRange(text_index, src.Count - count);
                }
            }
            else
                curr_text_dir = src;

            if (curr_data_dir.Count > 1)
                curr_data_dir.RemoveAt(0);
            curr_text_dir.RemoveAt(0);

            if (curr_text_dir.Count > 0 && curr_text_dir[0] == ".globl main")
                curr_text_dir.RemoveAt(0);
            else
                Shartilities.Log(Shartilities.LogType.ERROR, $"main is not defined in the assembly program\n", 1);
            return (curr_data_dir, curr_text_dir);
        }
        public static Program AssembleProgram(string src)
        {
            List<string> splitted = [.. src.Split('\n')];
            List<int> lines = [.. Enumerable.Range(1, splitted.Count)];
            List<(int, string)> code = [.. lines.Zip(splitted)];

            for (int i = 0; i < code.Count; i++)
            {
                int index = code[i].Item2.IndexOf('#');
                if (index != -1)
                    code[i] = (code[i].Item1, code[i].Item2.Remove(index));
            }
            code.RemoveAll(x => string.IsNullOrEmpty(x.Item2) || string.IsNullOrWhiteSpace(x.Item2));
            for (int i = 0; i < code.Count; i++)
            {
                code[i] = (code[i].Item1, code[i].Item2.Trim());
            }


            foreach ((int, string) inst in code)
            {
                Console.WriteLine($"line: {inst.Item1} , inst: `{inst.Item2}`");
            }



            //(List<string> data_dir, List<string> text_dir) = Get_directives(src);
            //curr_data_dir = data_dir;
            //curr_text_dir = text_dir;
            //(List<string> DM_INIT1, List<string> DM1, List<KeyValuePair<string, int>> addresses) = assemble_data_dir(curr_data_dir);
            //List<string> DM_INIT = DM_INIT1;
            //List<string> DM = DM1;
            //Assemble(addresses);


            //foreach (KeyValuePair<string, int> address in addresses)
            //{
            //    for (int i = 0; i < curr_text_dir.Count; i++)
            //    {
            //        curr_text_dir[i] = Replace(curr_text_dir[i], $@"\b{Escape(address.Key)}\b", address.Value.ToString());
            //    }
            //}
            //Assembler.Assembler assembler = new();
            //Assembler.Program program = assembler.AssembleProgram(curr_text_dir);
            //m_prog = program;
            //curr_insts = GetInstsAsText(m_prog);


            //p.Add("HLT");
            Shartilities.TODO("AssembleProgram");
            return new();
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
