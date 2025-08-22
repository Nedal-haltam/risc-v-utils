using static LibUtils;
namespace Assembler
{
    public static class Assembler
    {
        static bool LOG_INSTRUCTIONS;
        static string SrcFilePath = "";
        static string GetRtypeInst(string mnem, string rs1, string rs2, string rd, int line)
        {
            Shartilities.Assert(rs1.Length == 5 && rs2.Length == 5 && rd.Length == 5, $"invalid format in instruction {mnem}, lengths are: rs1={rs1.Length}, rs2={rs2.Length}, rd={rd.Length}");
            if (!Infos.ContainsKey(mnem))
                Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: unsupported instruction {mnem}\n", 1);
            InstInfo info = Infos[mnem];
            if (LOG_INSTRUCTIONS)
            {
                Console.WriteLine($"{mnem}:");
                Console.WriteLine($"    {"opcode", -7}: {info.Opcode}");
                Console.WriteLine($"    {"Funct3", -7}: {info.Funct3}");
                Console.WriteLine($"    {"Funct7", -7}: {info.Funct7}");
                Console.WriteLine($"    {"rs1"   , -7}: {rs1}");
                Console.WriteLine($"    {"rs2"   , -7}: {rs2}");
                Console.WriteLine($"    {"rd"    , -7}: {rd}");
            }
            return info.Funct7 + rs2 + rs1 + info.Funct3 + rd + info.Opcode;
        }
        static string GetItypeInst(string mnem, string imm12, string rs1, string rd, int line)
        {
            Shartilities.Assert(imm12.Length == 12 && rs1.Length == 5 && rd.Length == 5, $"invalid format in instruction {mnem}, lengths are: imm12={imm12.Length}, rs1={rs1.Length}, rd={rd.Length}");
            if (!Infos.ContainsKey(mnem))
                Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: unsupported instruction {mnem}\n", 1);
            InstInfo info = Infos[mnem];
            if (LOG_INSTRUCTIONS)
            {
                Console.WriteLine($"{mnem}:");
                Console.WriteLine($"    {"opcode", -7}: {info.Opcode}");
                Console.WriteLine($"    {"Funct3", -7}: {info.Funct3}");
                Console.WriteLine($"    {"Funct7", -7}: {info.Funct7}");
                Console.WriteLine($"    {"rs1"   , -7}: {rs1}");
                Console.WriteLine($"    {"imm"   , -7}: {imm12}");
                Console.WriteLine($"    {"rd"    , -7}: {rd}");
            }
            return imm12 + rs1 + info.Funct3 + rd + info.Opcode;
        }
        static string GetStypeInst(string mnem, string imm12, string rs1, string rs2, int line)
        {
            Shartilities.Assert(imm12.Length == 12 && rs1.Length == 5 && rs2.Length == 5, $"invalid format in instruction {mnem}, lengths are: imm12={imm12.Length}, rs1={rs1.Length}, rs2={rs2.Length}");
            if (!Infos.ContainsKey(mnem))
                Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: unsupported instruction {mnem}\n", 1);
            InstInfo info = Infos[mnem];
            if (LOG_INSTRUCTIONS)
            {
                Console.WriteLine($"{mnem}:");
                Console.WriteLine($"    {"opcode", -7}: {info.Opcode}");
                Console.WriteLine($"    {"Funct3", -7}: {info.Funct3}");
                Console.WriteLine($"    {"Funct7", -7}: {info.Funct7}");
                Console.WriteLine($"    {"rs1"   , -7}: {rs1}");
                Console.WriteLine($"    {"rs2"   , -7}: {rs2}");
                Console.WriteLine($"    {"imm"   , -7}: {imm12}");
            }
            return LibUtils.GetFromIndexLittle(imm12, 11, 5) + rs2 + rs1 + info.Funct3 + LibUtils.GetFromIndexLittle(imm12, 4, 0) + info.Opcode;
        }
        static string GetUtypeInst(string mnem, string imm20, string rd, int line)
        {
            InstInfo info = Infos[mnem];
            Shartilities.Assert(imm20.Length == 20 && rd.Length == 5, $"invalid format in instruction {mnem}, lengths are: imm20={imm20.Length}, rd={rd.Length}");
            if (!Infos.ContainsKey(mnem))
                Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: unsupported instruction {mnem}\n", 1);
            if (LOG_INSTRUCTIONS)
            {
                Console.WriteLine($"{mnem}:");
                Console.WriteLine($"    {"opcode", -7}: {info.Opcode}");
                Console.WriteLine($"    {"Funct3", -7}: {info.Funct3}");
                Console.WriteLine($"    {"Funct7", -7}: {info.Funct7}");
                Console.WriteLine($"    {"imm", -7}: {imm20}");
                Console.WriteLine($"    {"rd", -7}: {rd}");
            }
            return imm20 + rd + info.Opcode;
        }

        static string GetRegisterIndex(string reg, int line)
        {
            if (reg.StartsWith('$') || reg.StartsWith('x'))
            {
                reg = reg[1..];
                if (byte.TryParse(reg, out byte Index) && 0 <= Index && Index <= 31)
                    return zext(Convert.ToString(Index, 2), 5);
            }
            else if (REG_LIST.TryGetValue(reg, out var index))
            {
                return index.Item1;
            }
            Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: invalid register {reg}\n", 1);
            return "";
        }
        static void CheckTokensCount(string mnem, int have, int want) => Shartilities.Assert(have == want, $"invalid {mnem} instruction number of tokens is not {want}");
        static List<string> Instruction2MachineCodes(Instruction inst)
        {
            // references:
            // - https://msyksphinz-self.github.io/riscv-isadoc/
            // - https://riscv.github.io/riscv-isa-manual/snapshot/unprivileged/
            List<string> ts = inst.m_tokens;
            string mnem = ts[0].ToLower();
            switch (mnem)
            {
                // start of R-TYPE instructions
                case "add":
                    {
                        // add rd,rs1,rs2
                        // x[rd] = x[rs1] + x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];

                    }
                case "sub":
                    {
                        // sub rd,rs1,rs2
                        // x[rd] = x[rs1] - x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "mul":
                    {
                        // mul rd,rs1,rs2
                        // x[rd] = x[rs1] × x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "sll":
                    {
                        // sll rd,rs1,rs2
                        // x[rd] = x[rs1] << x[rs2]
                        // NOTE: Performs logical left shift on the value in register rs1 by the shift amount held in the
                        // ```lower 5 bits of register rs2```
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "slt":
                    {
                        // slt rd,rs1,rs2
                        // x[rd] = x[rs1] <s x[rs2]
                        // x[rd] = (signed(x[rs1]) < signed(x[rs2])) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "seq":
                    {
                        // seq rd,rs1,rs2
                        // x[rd] = x[rs1] == x[rs2]
                        // x[rd] = (x[rs1] == x[rs2]) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "sne":
                    {
                        // sne rd,rs1,rs2
                        // x[rd] = x[rs1] != x[rs2]
                        // x[rd] = (x[rs1] != x[rs2]) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "sltu":
                    {
                        // sltu rd,rs1,rs2
                        // x[rd] = x[rs1] <u x[rs2]
                        // x[rd] = (unsigned(x[rs1]) < unsigned(x[rs2])) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "xor":
                    {
                        // xor rd,rs1,rs2
                        // x[rd] = x[rs1] ^ x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "div":
                    {
                        // div rd,rs1,rs2
                        // x[rd] = x[rs1] %s x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "srl":
                    {
                        // srl rd,rs1,rs2
                        // x[rd] = x[rs1] >>u x[rs2]
                        // NOTE: Logical right shift on the value in register rs1 by the shift amount held in the
                        // ```lower 5 bits of register rs2```
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "sra":
                    {
                        // sra rd,rs1,rs2
                        // x[rd] = x[rs1] >>s x[rs2]
                        // NOTE: Performs arithmetic right shift on the value in register rs1 by the shift amount held in the
                        // lower 5 bits of register rs2
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "divu":
                    {
                        // divu rd,rs1,rs2
                        // x[rd] = x[rs1] /u x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "or":
                    {
                        // or rd,rs1,rs2
                        // x[rd] = x[rs1] | x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "rem":
                    {
                        // rem rd,rs1,rs2
                        // x[rd] = x[rs1] %s x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "and":
                    {
                        // and rd,rs1,rs2
                        // x[rd] = x[rs1] & x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                case "remu":
                    {
                        // remu rd,rs1,rs2
                        // x[rd] = x[rs1] %u x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst(mnem, rs1, rs2, rd, inst.m_line)];
                    }
                // end of R-TYPE instructions
                // start of I-TYPE instructions
                case "addi":
                    {
                        // addi rd,rs1,imm
                        // x[rd] = x[rs1] + SignExtended(immediate)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(StringToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd, inst.m_line)];
                    }
                case "slti":
                    {
                        // slti rd,rs1,imm
                        // x[rd] = x[rs1] <s SignExtended(immediate)
                        // x[rd] = (signed(x[rs1]) < signed(SignExtended(immediate))) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(StringToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd, inst.m_line)];
                    }
                case "sltiu":
                    {
                        // sltiu rd,rs1,imm
                        // x[rd] = x[rs1] <u SignExtended(immediate)
                        // the difference is that the numbers are treated as unsigned instead
                        // x[rd] = (unsigned(x[rs1]) < unsigned(SignExtended(immediate))) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(StringToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd, inst.m_line)];
                    }
                case "xori":
                    {
                        // xori rd,rs1,imm
                        // x[rd] = x[rs1] ^ SignExtended(immediate)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(StringToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd, inst.m_line)];
                    }
                case "ori":
                    {
                        // ori rd,rs1,imm
                        // x[rd] = x[rs1] | SignExtended(immediate)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(StringToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd, inst.m_line)];
                    }
                case "andi":
                    {
                        // andi rd,rs1,imm
                        // x[rd] = x[rs1] & SignExtended(immediate)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(StringToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd, inst.m_line)];
                    }
                case "slli":
                    {
                        // slli rd,rs1,shamt
                        // x[rd] = x[rs1] << shamt
                        // NOTE: in RV64, bit-25 in the instruction maching code is used to shamt[5] so the shamt in this case is 6-bits,
                        // but in RV32 the shamt is 5-bits
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string imm = ts[3];
                        imm = zext(GetFromIndexLittle(StringToBin(imm), 5, 0), 12);
                        return [GetItypeInst(mnem, imm, rs1, rd, inst.m_line)];
                    }
                case "srli":
                    {
                        // srli rd,rs1,shamt
                        // x[rd] = x[rs1] >>u shamt
                        // NOTE: in RV64, bit-25 in the instruction maching code is used to shamt[5] so the shamt in this case is 6-bits,
                        // but in RV32 the shamt is 5-bits
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string imm = ts[3];
                        imm = zext(GetFromIndexLittle(StringToBin(imm), 5, 0), 12);
                        return [GetItypeInst(mnem, imm, rs1, rd, inst.m_line)];
                    }
                case "srai":
                    {
                        // srai rd,rs1,shamt
                        // x[rd] = x[rs1] >>s shamt
                        // NOTE: in RV64, bit-25 in the instruction maching code is used to shamt[5] so the shamt in this case is 6-bits,
                        // but in RV32 the shamt is 5-bits
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string imm = ts[3];
                        imm = zext(GetFromIndexLittle(StringToBin(imm), 5, 0), 12);
                        imm = string.Concat(imm.AsSpan()[..1], "1", imm.AsSpan(2));
                        return [GetItypeInst(mnem, imm, rs1, rd, inst.m_line)];
                    }
                case "ecall":
                    {
                        // ecall
                        // RaiseException(EnvironmentCall)
                        return [GetItypeInst(mnem, zext("", 12), GetRegisterIndex("zero", inst.m_line), GetRegisterIndex("zero", inst.m_line), inst.m_line)];
                    }
                case "lb":
                    {
                        // lb rd,offset(rs1)
                        // x[rd] = SignExtended(M[x[rs1] + SignExtended(offset)][7:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3], inst.m_line);
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd, inst.m_line)];
                    }
                case "lh":
                    {
                        // lh rd,offset(rs1)
                        // x[rd] = SignExtended(M[x[rs1] + SignExtended(offset)][15:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3], inst.m_line);
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd, inst.m_line)];
                    }
                case "lw":
                    {
                        // lw rd,offset(rs1)
                        // x[rd] = SignExtended(M[x[rs1] + SignExtended(offset)][31:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3], inst.m_line);
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd, inst.m_line)];
                    }
                case "ld":
                    {
                        // ld rd,offset(rs1)
                        // x[rd] = SignExtended(M[x[rs1] + SignExtended(offset)][63:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3], inst.m_line);
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd, inst.m_line)];
                    }
                case "lbu":
                    {
                        // lbu rd,offset(rs1)
                        // x[rd] = ZeroExtended(M[x[rs1] + SignExtended(offset)][7:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3], inst.m_line);
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd, inst.m_line)];
                    }
                case "lhu":
                    {
                        // lhu rd,offset(rs1)
                        // x[rd] = ZeroExtended(M[x[rs1] + SignExtended(offset)][15:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3], inst.m_line);
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd, inst.m_line)];
                    }
                case "jalr":
                    {
                        // jalr rd,rs1,offset
                        // t = pc+4;
                        // pc = (x[rs1] + SignExtended(offset)) & ~1;
                        // x[rd] = t
                        // NOTE: the steps above are important and should be implemented exactly as shown
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string offset = ts[3];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd, inst.m_line)];
                    }
                // end of I-TYPE instructions
                // start of S-TYPE instructions
                case "sb":
                    {
                        // sb rs2,offset(rs1)
                        // M[x[rs1] + SignExtended(offset)] = x[rs2][7:0]
                        // NOTE: Store 8-bit, values from the
                        // ```low bits of register rs2 to memory.```
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs2 = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3], inst.m_line);
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetStypeInst(mnem, offset, rs1, rs2, inst.m_line)];
                    }
                case "sh":
                    {
                        // sh rs2,offset(rs1)
                        // M[x[rs1] + SignExtended(offset)] = x[rs2][15:0]
                        // NOTE: Store 16-bit, values from the
                        // ```low bits of register rs2 to memory.```
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs2 = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3], inst.m_line);
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetStypeInst(mnem, offset, rs1, rs2, inst.m_line)];
                    }
                case "sw":
                    {
                        // sw rs2,offset(rs1)
                        // M[x[rs1] + SignExtended(offset)] = x[rs2][31:0]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs2 = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3], inst.m_line);
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetStypeInst(mnem, offset, rs1, rs2, inst.m_line)];
                    }
                case "sd":
                    {
                        // sd rs2,offset(rs1)
                        // M[x[rs1] + SignExtended(offset)] = x[rs2][63:0]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs2 = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3], inst.m_line);
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 11, 0);
                        return [GetStypeInst(mnem, offset, rs1, rs2, inst.m_line)];
                    }
                case "beq":
                    {
                        // beq rs1,rs2,offset
                        // if (rs1 == rs2) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs1 = GetRegisterIndex(ts[1], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[2], inst.m_line);
                        string offset = ts[3];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 12, 1);
                        return [GetStypeInst(mnem, offset, rs1, rs2, inst.m_line)];
                    }
                case "bne":
                    {
                        // bne rs1,rs2,offset
                        // if (rs1 != rs2) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs1 = GetRegisterIndex(ts[1], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[2], inst.m_line);
                        string offset = ts[3];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 12, 1);
                        return [GetStypeInst(mnem, offset, rs1, rs2, inst.m_line)];
                    }
                case "blt":
                    {
                        // blt rs1,rs2,offset
                        // if (rs1 <s rs2) pc += SignExtended(offset)
                        // if (signed(rs1) <s signed(rs2)) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs1 = GetRegisterIndex(ts[1], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[2], inst.m_line);
                        string offset = ts[3];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 12, 1);
                        return [GetStypeInst(mnem, offset, rs1, rs2, inst.m_line)];
                    }
                case "bge":
                    {
                        // bge rs1,rs2,offset
                        // if (rs1 >=s rs2) pc += SignExtended(offset)
                        // if (signed(rs1) >=s signed(rs2)) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs1 = GetRegisterIndex(ts[1], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[2], inst.m_line);
                        string offset = ts[3];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 12, 1);
                        return [GetStypeInst(mnem, offset, rs1, rs2, inst.m_line)];
                    }
                case "bltu":
                    {
                        // bltu rs1,rs2,offset
                        // if (rs1 <u rs2) pc += sext(offset)
                        // if (unsigned(rs1) <u unsigned(rs2)) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs1 = GetRegisterIndex(ts[1], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[2], inst.m_line);
                        string offset = ts[3];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 12, 1);
                        return [GetStypeInst(mnem, offset, rs1, rs2, inst.m_line)];
                    }
                case "bgeu":
                    {
                        // bgeu rs1,rs2,offset
                        // if (rs1 >=u rs2) pc += sext(offset)
                        // if (unsigned(rs1) >=u unsigned(rs2)) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs1 = GetRegisterIndex(ts[1], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[2], inst.m_line);
                        string offset = ts[3];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 12, 1);
                        return [GetStypeInst(mnem, offset, rs1, rs2, inst.m_line)];
                    }
                // end of S-TYPE instructions
                // start of U-TYPE instructions
                case "lui":
                    {
                        // lui rd,imm
                        // x[rd] = SignExtended(immediate[31:12] << 12)
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string imm = ts[2];
                        imm = LibUtils.GetFromIndexLittle(StringToBin(imm), 31, 12);
                        return [GetUtypeInst(mnem, imm, rd, inst.m_line)];
                    }
                case "auipc":
                    {
                        // auipc rd,imm
                        // x[rd] = pc + SignExtended(immediate[31:12] << 12)
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string imm = ts[2];
                        imm = LibUtils.GetFromIndexLittle(StringToBin(imm), 31, 12);
                        return [GetUtypeInst(mnem, imm, rd, inst.m_line)];
                    }
                case "jal":
                    {
                        // jal rd,offset
                        // x[rd] = pc+4; pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 20, 1); // offset = offset[20:1]
                        return [GetUtypeInst(mnem, offset, rd, inst.m_line)];
                    }
                case "addi20u": // user defined instruction
                    {
                        // addi20u rd,imm
                        // x[rd] = x[rd] + ZeroExtended(imm[19:0])
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string imm = ts[2];
                        imm = LibUtils.GetFromIndexLittle(StringToBin(imm), 19, 0);
                        return [GetUtypeInst(mnem, imm, rd, inst.m_line)];
                    }
                // end of U-TYPE instructions
                // start of pseudo-Instructions
                //----------------------------------------------------------------------------------------------------------------------------------
                case "la":
                    {
                        // la rd,symbol → ↓
                        // lui rd, symbol[31:12] + (1 (if symbol[11:0] < 0))
                        // addi rd, rd, symbol[11:0]
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string symbol = ts[2];
                        symbol = StringToBin(symbol);
                        // to account for the sign extension of symbol[11:0] in the addi instruction
                        string imm12 = GetFromIndexLittle(symbol, 11, 0);
                        string imm20_64 = GetFromIndexLittle(symbol, 31, 12);
                        if (imm12[0] == '1')
                            imm20_64 = GetFromIndexLittle(StringToBin((Convert.ToInt64(zext(imm20_64, 64), 2) + 1).ToString()), 19, 0);
                        return [
                            GetUtypeInst("lui", imm20_64, rd, inst.m_line),
                            GetItypeInst("addi" , imm12, rd, rd, inst.m_line),
                        ];
                    }
                case "li":
                    {
                        // li rd,immediate
                        // x[rd] = SignExtended(immediate)
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string imm = ts[2];
                        imm = StringToBin(imm);
                        return [
                                GetItypeInst("addi", zext("", 12), GetRegisterIndex("zero", inst.m_line), rd, inst.m_line),
                                GetUtypeInst("addi20u", GetFromIndexLittle(imm, 63, 44), rd, inst.m_line),
                                GetItypeInst("slli", "000000010100", rd, rd, inst.m_line),
                                GetUtypeInst("addi20u", GetFromIndexLittle(imm, 43, 24), rd, inst.m_line),
                                GetItypeInst("slli", "000000010100", rd, rd, inst.m_line),
                                GetUtypeInst("addi20u", GetFromIndexLittle(imm, 23, 4), rd, inst.m_line),
                                GetItypeInst("slli", "000000000100", rd, rd, inst.m_line),
                                GetUtypeInst("addi20u", zext(GetFromIndexLittle(imm, 3, 0), 20), rd, inst.m_line),
                            ];
                    }
                case "nop":
                    {
                        // nop
                        // nothing
                        return [GetItypeInst("addi", zext("", 12), GetRegisterIndex("zero", inst.m_line), GetRegisterIndex("zero", inst.m_line), inst.m_line)];
                    }
                case "mv":
                    {
                        // mv rd,rs1
                        // x[rd] = x[rs1]
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        return [GetItypeInst("addi", zext("", 12), rs1, rd, inst.m_line)];
                    }
                case "not":
                    {
                        // not rd,rs1
                        // x[rd] = x[rs1] ^ SignExtended(-1)
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        return [GetItypeInst("xori", sext("1", 12), rs1, rd, inst.m_line)];
                    }
                case "neg":
                    {
                        // neg rd,rs2
                        // x[rd] = -x[rs2]
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[2], inst.m_line);
                        return [GetRtypeInst("sub", GetRegisterIndex("zero", inst.m_line), rs2, rd, inst.m_line)];
                    }
                case "snez":
                    {
                        // snez rd,rs1
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        return [GetRtypeInst("sne", rs1, GetRegisterIndex("zero", inst.m_line), rd, inst.m_line)];
                    }
                case "seqz":
                    {
                        // snez rd,rs1
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        return [GetRtypeInst("seq", rs1, GetRegisterIndex("zero", inst.m_line), rd, inst.m_line)];
                    }
                case "sgt":
                    {
                        // sgt rd,rs1,rs2
                        // x[rd] = x[rs1] >s x[rs2]
                        // x[rd] = (signed(x[rs1]) > signed(x[rs2])) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1], inst.m_line);
                        string rs1 = GetRegisterIndex(ts[2], inst.m_line);
                        string rs2 = GetRegisterIndex(ts[3], inst.m_line);
                        return [GetRtypeInst("slt", rs2, rs1, rd, inst.m_line)]; // flipped the operands
                    }
                case "call":
                    {
                        // call offset
                        // pc += SignExtended(offset)
                        // call offset -> jal ra,offset
                        CheckTokensCount(mnem, ts.Count, 2);
                        string offset = ts[1];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 20, 1); // offset = offset[20:1]
                        return [GetUtypeInst("jal", offset, GetRegisterIndex("ra", inst.m_line), inst.m_line)];
                    }
                case "j":
                    {
                        // j label
                        // pc = label -> pc = pc + (label - pc) = pc + (offset)
                        // j offset -> jal x0,offset
                        CheckTokensCount(mnem, ts.Count, 2);
                        string offset = ts[1];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 20, 1); // offset = offset[20:1]
                        return [GetUtypeInst("jal", offset, GetRegisterIndex("zero", inst.m_line), inst.m_line)];
                    }
                case "ret":
                    {
                        // ret
                        // pc = ra
                        CheckTokensCount(mnem, ts.Count, 1);
                        return [GetItypeInst("jalr", zext("", 12), GetRegisterIndex("ra", inst.m_line), GetRegisterIndex("zero", inst.m_line), inst.m_line)];
                    }
                case "jr":
                    {
                        // jr rs1
                        // pc = x[rs1]
                        // jr rs1 -> jalr x0,rs1,0
                        CheckTokensCount(mnem, ts.Count, 2);
                        string rs1 = GetRegisterIndex(ts[1], inst.m_line);
                        return [GetItypeInst("jalr", zext("", 12), rs1, GetRegisterIndex("zero", inst.m_line), inst.m_line)];
                    }
                case "beqz":
                    {
                        // beqz rs1,offset
                        // if (rs1 == rs2) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rs1 = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 12, 1);
                        return [GetStypeInst("beq", offset, rs1, GetRegisterIndex("zero", inst.m_line), inst.m_line)];
                    }
                case "bltz":
                    {
                        // bltz rs1,offset
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rs1 = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 12, 1);
                        return [GetStypeInst("blt", offset, rs1, GetRegisterIndex("zero", inst.m_line), inst.m_line)];
                    }
                case "bgez":
                    {
                        // bgez rs1,offset
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rs1 = GetRegisterIndex(ts[1], inst.m_line);
                        string offset = ts[2];
                        offset = LibUtils.GetFromIndexLittle(StringToBin(offset), 12, 1);
                        return [GetStypeInst("bge", offset, rs1, GetRegisterIndex("zero", inst.m_line), inst.m_line)];
                    }
                default:
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{inst.m_line}:1: invalid instruction mnemonic {mnem}\n", 1);
                        return [];
                    }
            }
        }
        static (List<(int, string)>, List<(int, string)>) GetTextDataSections(List<(int, string)> src)
        {
            int TextIndex = src.FindIndex(x => x.Item2 == ".section .text");
            int DataIndex = src.FindIndex(x => x.Item2 == ".section .data");

            List<(int, string)> DataSection = [];
            List<(int, string)> TextSection = [];

            if (TextIndex == -1)
                Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:1:1: text directive doesn't exist\n", 1);

            if (DataIndex == -1)
            {
                TextSection = src;
            }
            else
            {
                int count = (int)MathF.Abs(TextIndex - DataIndex);
                if (DataIndex > TextIndex)
                {
                    DataSection = src.GetRange(DataIndex, src.Count - count);
                    TextSection = src.GetRange(TextIndex, count);
                }
                else
                {
                    DataSection = src.GetRange(DataIndex, count);
                    TextSection = src.GetRange(TextIndex, src.Count - count);
                }
                DataSection.RemoveAt(0); // remove: .section .data
            }

            TextSection.RemoveAt(0); // remove: .section .text
            if (TextSection.Count > 1 && TextSection[0].Item2 == ".globl main" && TextSection[1].Item2 == "main:")
                TextSection.RemoveAt(0);
            else
                Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:1:1: main is not defined in the assembly program\n", 1);

            return (TextSection, DataSection);
        }
        static string ParseLabel(string label, int line)
        {
            label = label[..^1].Trim();
            if (label.Split(' ').Length > 1)
                Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: invalid label syntax {label}\n", 1);
            return label;
        }
        static uint GetInstructionSize(string mnem)
        {
            return mnem.ToLower() switch
            {
                "li" => 32,
                "la" => 8,
                _ => 4,
            };
        }
        public static Program AssembleProgram(string FilePath, bool LOG_INST_FLAG)
        {
            LOG_INSTRUCTIONS = LOG_INST_FLAG;
            string src = File.ReadAllText(FilePath);
            SrcFilePath = FilePath;

            List<string> splitted = [.. src.Split('\n')];
            List<int> lines = [.. Enumerable.Range(1, splitted.Count)];
            List<(int, string)> code = [.. lines.Zip(splitted)];

            for (int i = 0; i < code.Count; i++)
            {
                // TODO: consume comments later not here, not like this
                // can do it while tokenizing
                if (!code[i].Item2.Contains(".string"))
                {
                    int index = code[i].Item2.IndexOf('#');
                    if (index != -1)
                        code[i] = (code[i].Item1, code[i].Item2[..index]);
                }
            }
            code.RemoveAll(x => string.IsNullOrEmpty(x.Item2) || string.IsNullOrWhiteSpace(x.Item2));
            for (int i = 0; i < code.Count; i++)
            {
                code[i] = (code[i].Item1, code[i].Item2.Trim());
            }

            (List<(int, string)> TextSection, List<(int, string)> DataSection) = GetTextDataSections(code);
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Program p = new();
            Dictionary<string, uint> Labels = [];
            uint LabelAddress = 0;
            for (int i = 0; i < TextSection.Count; i++)
            {
                int LineNumber = TextSection[i].Item1;
                string instruction = TextSection[i].Item2;

                string PossibleLabel = instruction.Trim();
                int ColonIndex = PossibleLabel.LastIndexOf(':');
                if (ColonIndex == PossibleLabel.Length - 1)
                {
                    Labels.Add(ParseLabel(PossibleLabel, LineNumber), LabelAddress);
                }
                else
                {
                    List<string> tokens = [.. instruction.Split(',')];
                    string t = tokens[0];
                    tokens.RemoveAt(0);
                    tokens.InsertRange(0, [.. t.Split(' ')]);
                    for (int j = 0; j < tokens.Count; j++) tokens[j] = tokens[j].Trim();
                    tokens.RemoveAll(x => string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x));

                    if (tokens[^1].Contains('('))
                    {
                        t = tokens[^1];
                        tokens = tokens[..^1];
                        List<string> ts = [.. t.Split('(', ')')];
                        for (int j = 0; j < ts.Count; j++) ts[j] = ts[j].Trim();
                        ts.RemoveAll(x => string.IsNullOrEmpty(x) || string.IsNullOrWhiteSpace(x));
                        tokens.AddRange(ts);
                    }
                    p.Instructions.Add(new(tokens, LineNumber));
                    LabelAddress += GetInstructionSize(tokens[0]);
                }
            }
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            Dictionary<string, ulong> DataSectionAddresses = [];
            ulong CurrentDataAddress = 0;
            for (int i = 0; i < DataSection.Count; i++)
            {
                int line = DataSection[i].Item1;
                string instruction = DataSection[i].Item2;

                string PossibleLabel = instruction.Trim();
                int ColonIndex = PossibleLabel.LastIndexOf(':');
                if (ColonIndex == PossibleLabel.Length - 1)
                {
                    DataSectionAddresses.Add(ParseLabel(PossibleLabel, line), CurrentDataAddress);
                }
                else if (instruction == ".section .bss")
                {
                    continue;
                }
                else
                {
                    int index = instruction.IndexOf(' ');
                    if (index == -1)
                        Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: invalid syntax in data section\n", 1);

                    string Directive = instruction[..index];
                    if (Directive == ".space")
                    {
                        List<string> data = [.. instruction[index..].Trim().Split(',')];
                        for (int j = 0; j < data.Count; j++) data[j] = data[j].Trim();
                        p.DataMemoryValues.Add(Directive);
                        p.DataMemoryValues.AddRange(data);
                        if (data.Count == 0)
                            Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: no expression was provided for .space directive\n", 1);
                        if (data.Count != 1)
                            Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: invalid number of expressions should be one for .space directive\n", 1);

                        if (!ulong.TryParse(data[0], out ulong value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: invalid expression in directive .spcace with the value {data[0]}\n", 1);
                        CurrentDataAddress += 1 * value;
                    }
                    else if (Directive == ".string")
                    {
                        string StringLit = instruction[index..].Trim();
                        if (StringLit.Length > 1 && StringLit[0] == '"' && StringLit[^1] == '"')
                        {
                            p.DataMemoryValues.Add(Directive);
                            int j = 1;
                            int len = 0;
                            List<string> temp = [];
                            while (j < StringLit.Length - 1)
                            {
                                char c = StringLit[j];
                                if (c == '\\')
                                {
                                    if (j + 1 < StringLit.Length - 1)
                                    {
                                        if (StringLit[j + 1] == 'n')
                                        {
                                            temp.Add("\n");
                                            j++;
                                            j++;
                                        }
                                    }
                                    else
                                    {
                                        Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: unsupported escape character in string literal {StringLit}\n", 1);
                                    }
                                }
                                else
                                {
                                    temp.Add($"{c}");
                                    j++;
                                }
                                len++;
                            }
                            temp.Add("\0");
                            len++;
                            p.DataMemoryValues.Add(len.ToString());
                            p.DataMemoryValues.AddRange(temp);
                            CurrentDataAddress += 1 * ((ulong)temp.Count);
                        }
                        else
                        {
                            Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: invalid string literal declaration: {StringLit}\n", 1);
                        }
                    }
                    else if (Directive == ".word")
                    {
                        List<string> data = [.. instruction[index..].Trim().Split(',')];
                        for (int j = 0; j < data.Count; j++) data[j] = data[j].Trim();
                        p.DataMemoryValues.Add(Directive);
                        p.DataMemoryValues.Add(data.Count.ToString());
                        p.DataMemoryValues.AddRange(data);
                        CurrentDataAddress += 4 * (ulong)data.Count;
                    }
                    else
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"{SrcFilePath}:{line}:1: unsupported data memory directive {Directive}\n", 1);
                    }
                }
            }
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            foreach (KeyValuePair<string, ulong> address in DataSectionAddresses)
            {
                for (int i = 0; i < p.Instructions.Count; i++)
                {
                    for (int j = 1; j < p.Instructions[i].m_tokens.Count; j++)
                    {
                        if (p.Instructions[i].m_tokens[j] == address.Key)
                        {
                            p.Instructions[i].m_tokens[j] = address.Value.ToString();
                        }
                    }
                }
            }
            
            foreach (KeyValuePair<string, uint> l in Labels)
            {
                uint AccumulatedAddress = 0;
                for (int i = 0; i < p.Instructions.Count; i++)
                {
                    for (int j = 1; j < p.Instructions[i].m_tokens.Count; j++)
                    {
                        if (p.Instructions[i].m_tokens[j] == l.Key)
                        {
                            int offset = (int)(l.Value - AccumulatedAddress);
                            p.Instructions[i].m_tokens[j] = offset.ToString();
                        }
                    }
                    AccumulatedAddress += GetInstructionSize(p.Instructions[i].m_tokens[0]);
                }
            }
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            for (int i = 0; i < p.Instructions.Count; i++)
            {
                p.MachineCodes.AddRange(Instruction2MachineCodes(p.Instructions[i]));
            }
            return p;
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
