using System.Globalization;
using static LibUtils;
namespace Assembler
{
    public static class Assembler
    {
        static string GetRtypeInst(string mnem, string rs1, string rs2, string rd)
        {
            Shartilities.Assert(rs1.Length == 5 && rs2.Length == 5 && rd.Length == 5, $"invalid format in instruction `{mnem}`, lengths are: rs1={rs1.Length}, rs2={rs2.Length}, rd={rd.Length}");
            if (!Infos.ContainsKey(mnem))
                Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
            InstInfo info = Infos[mnem];
            return info.Funct7 + rs2 + rs1 + info.Funct3 + rd + info.Opcode;
        }
        static string GetItypeInst(string mnem, string imm12, string rs1, string rd)
        {
            Shartilities.Assert(imm12.Length == 12 && rs1.Length == 5 && rd.Length == 5, $"invalid format in instruction `{mnem}`, lengths are: imm12={imm12.Length}, rs1={rs1.Length}, rd={rd.Length}");
            if (!Infos.ContainsKey(mnem))
                Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
            InstInfo info = Infos[mnem];
            return imm12 + rs1 + info.Funct3 + rd + info.Opcode;
        }
        static string GetStypeInst(string mnem, string imm12, string rs1, string rs2)
        {
            Shartilities.Assert(imm12.Length == 12 && rs1.Length == 5 && rs2.Length == 5, $"invalid format in instruction `{mnem}`, lengths are: imm12={imm12.Length}, rs1={rs1.Length}, rs2={rs2.Length}");
            if (!Infos.ContainsKey(mnem))
                Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
            InstInfo info = Infos[mnem];
            return LibUtils.GetFromIndexLittle(imm12, 11, 5) + rs2 + rs1 + info.Funct3 + LibUtils.GetFromIndexLittle(imm12, 4, 0) + info.Opcode;
        }
        static string GetUtypeInst(string mnem, string imm20, string rd)
        {
            Shartilities.Assert(imm20.Length == 20 && rd.Length == 5, $"invalid format in instruction `{mnem}`, lengths are: imm20={imm20.Length}, rd={rd.Length}");
            if (!Infos.ContainsKey(mnem))
                Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported instruction `{mnem}`\n", 1);
            return imm20 + rd + Infos[mnem].Opcode;
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
        static void CheckTokensCount(string mnem, int have, int want) => Shartilities.Assert(have == want, $"invalid `{mnem}` instruction");
        static List<string> Instruction2MachineCodes(Instruction inst)
        {
            // references:
            // - https://msyksphinz-self.github.io/riscv-isadoc/
            // - https://riscv.github.io/riscv-isa-manual/snapshot/unprivileged/
            List<string> ts = inst.m_tokens;
            string mnem = ts[0].ToLower();
            // TODO:
            //      - support negative numbers
            switch (mnem)
            {
                case "lui":
                    {
                        // lui rd,imm
                        // x[rd] = SignExtended(immediate[31:12] << 12)
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1]);
                        string imm = ts[2];
                        imm = LibUtils.GetFromIndexLittle(GetImmediateToBin(imm), 31, 12);
                        return [GetUtypeInst(mnem, imm, rd)];
                    }
                case "auipc":
                    {
                        // auipc rd,imm
                        // x[rd] = pc + SignExtended(immediate[31:12] << 12)
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1]);
                        string imm = ts[2];
                        imm = LibUtils.GetFromIndexLittle(GetImmediateToBin(imm), 31, 12);
                        return [GetUtypeInst(mnem, imm, rd)];
                    }
                case "la":
                    {
                        // la rd,symbol → ↓
                        // auipc rd, symbol[31:12] + 1
                        // addi rd, rd, symbol[11:0]
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1]);
                        string symbol = ts[2];
                        symbol = GetImmediateToBin(symbol);
                        //// to account for the sign extension of symbol[11:0] in the `addi` instruction
                        //// TODO: should we handle it in software (easier) or in hardware (not hard)
                        //symbol = Convert.ToString(Convert.ToUInt32(symbol, 2) + (uint)(symbol[20] == '1' ? 1 << 12 : 0), 2).PadLeft(32, '0');
                        return [
                            GetUtypeInst("lui", LibUtils.GetFromIndexLittle(GetImmediateToBin(symbol), 31, 12), rd),
                            GetItypeInst("addi" , LibUtils.GetFromIndexLittle(symbol, 11, 0), rd, rd),
                            ];
                    }
                case "addi":
                    {
                        // addi rd,rs1,imm
                        // x[rd] = x[rs1] + SignExtended(immediate)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(GetImmediateToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "li":
                    {
                        // li rd,immediate
                        // x[rd] = SignExtended(immediate)
                        // TODO: fix and implement it using the Myriad sequences
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1]);
                        string imm = ts[2];
                        imm = LibUtils.GetFromIndexLittle(GetImmediateToBin(imm), 11, 0);
                        return [GetItypeInst("addi", imm, GetRegisterIndex("zero"), rd)];
                    }
                case "nop":
                    {
                        // nop
                        // nothing
                        return [GetItypeInst("addi", "".PadLeft(12, '0'), GetRegisterIndex("zero"), GetRegisterIndex("zero"))];
                    }
                case "mv":
                    {
                        // mv rd,rs1
                        // x[rd] = x[rs1]
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        return [GetItypeInst("addi", "".PadLeft(12, '0'), rs1, rd)];
                    }
                case "slti":
                    {
                        // slti rd,rs1,imm
                        // x[rd] = x[rs1] <s SignExtended(immediate)
                        // x[rd] = (signed(x[rs1]) < signed(SignExtended(immediate))) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(GetImmediateToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "sltiu":
                    {
                        // sltiu rd,rs1,imm
                        // x[rd] = x[rs1] <u SignExtended(immediate)
                        // the difference is that the numbers are treated as unsigned instead
                        // x[rd] = (unsigned(x[rs1]) < unsigned(ZeroExtended(immediate))) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(GetImmediateToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "xori":
                    {
                        // xori rd,rs1,imm
                        // x[rd] = x[rs1] ^ SignExtended(immediate)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(GetImmediateToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "not":
                    {
                        // not rd,rs1
                        // x[rd] = x[rs1] ^ SignExtended(-1)
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rd = GetRegisterIndex(ts[1]);
                        return [GetItypeInst("xori", "".PadLeft(12, '1'), rs1, rd)];
                    }
                case "ori":
                    {
                        // ori rd,rs1,imm
                        // x[rd] = x[rs1] | SignExtended(immediate)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(GetImmediateToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "andi":
                    {
                        // andi rd,rs1,imm
                        // x[rd] = x[rs1] & SignExtended(immediate)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string imm = ts[3];
                        imm = LibUtils.GetFromIndexLittle(GetImmediateToBin(imm), 11, 0);
                        return [GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "slli":
                    {
                        // slli rd,rs1,shamt
                        // x[rd] = x[rs1] << shamt
                        // NOTE: in RV64, bit-25 in the instruction maching code is used to shamt[5] so the shamt in this case is 6-bits,
                        // but in RV32 the shamt is 5-bits
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string imm = ts[3];
                        imm = GetImmediateToBin(imm).Substring(31 - 4, 5).PadLeft(12, '0');
                        return [GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "srli":
                    {
                        // srli rd,rs1,shamt
                        // x[rd] = x[rs1] >>u shamt
                        // NOTE: in RV64, bit-25 in the instruction maching code is used to shamt[5] so the shamt in this case is 6-bits,
                        // but in RV32 the shamt is 5-bits
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string imm = ts[3];
                        imm = GetImmediateToBin(imm).Substring(31 - 4, 5).PadLeft(12, '0');
                        return [GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "srai":
                    {
                        // srai rd,rs1,shamt
                        // x[rd] = x[rs1] >>s shamt
                        // NOTE: in RV64, bit-25 in the instruction maching code is used to shamt[5] so the shamt in this case is 6-bits,
                        // but in RV32 the shamt is 5-bits
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string imm = ts[3];
                        imm = GetImmediateToBin(imm).Substring(31 - 4, 5).PadLeft(12, '0');
                        imm = string.Concat(imm.AsSpan()[..1], "1", imm.AsSpan(2));
                        return [GetItypeInst(mnem, imm, rs1, rd)];
                    }
                case "add":
                    {
                        // add rd,rs1,rs2
                        // x[rd] = x[rs1] + x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst(mnem, rs1, rs2, rd)];

                    }
                case "sub":
                    {
                        // sub rd,rs1,rs2
                        // x[rd] = x[rs1] - x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "sll":
                    {
                        // sll rd,rs1,rs2
                        // x[rd] = x[rs1] << x[rs2]
                        // NOTE: Performs logical left shift on the value in register rs1 by the shift amount held in the
                        // ```lower 5 bits of register rs2```
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "slt":
                    {
                        // slt rd,rs1,rs2
                        // x[rd] = x[rs1] <s x[rs2]
                        // x[rd] = (signed(x[rs1]) < signed(x[rs2])) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "sgt":
                    {
                        // sgt rd,rs1,rs2
                        // x[rd] = x[rs1] >s x[rs2]
                        // x[rd] = (signed(x[rs1]) > signed(x[rs2])) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst("slt", rs2, rs1, rd)]; // flipped the operands
                    }
                case "sltu":
                    {
                        // sltu rd,rs1,rs2
                        // x[rd] = x[rs1] <u x[rs2]
                        // x[rd] = (unsigned(x[rs1]) < unsigned(x[rs2])) ? 1 : 0
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "xor":
                    {
                        // xor rd,rs1,rs2
                        // x[rd] = x[rs1] ^ x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "srl":
                    {
                        // srl rd,rs1,rs2
                        // x[rd] = x[rs1] >>u x[rs2]
                        // NOTE: Logical right shift on the value in register rs1 by the shift amount held in the
                        // ```lower 5 bits of register rs2```
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "sra":
                    {
                        // sra rd,rs1,rs2
                        // x[rd] = x[rs1] >>s x[rs2]
                        // NOTE: Performs arithmetic right shift on the value in register rs1 by the shift amount held in the
                        // ```lower 5 bits of register rs2```
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "or":
                    {
                        // or rd,rs1,rs2
                        // x[rd] = x[rs1] | x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "and":
                    {
                        // and rd,rs1,rs2
                        // x[rd] = x[rs1] & x[rs2]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string rs2 = GetRegisterIndex(ts[3]);
                        return [GetRtypeInst(mnem, rs1, rs2, rd)];
                    }
                case "ecall":
                    {
                        // ecall
                        // RaiseException(EnvironmentCall)
                        return [GetItypeInst(mnem, "".PadLeft(12, '0'), GetRegisterIndex("zero"), GetRegisterIndex("zero"))];
                    }
                case "lb":
                    {
                        // lb rd,offset(rs1)
                        // x[rd] = SignExtended(M[x[rs1] + SignExtended(offset)][7:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3]);
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "lh":
                    {
                        // lh rd,offset(rs1)
                        // x[rd] = SignExtended(M[x[rs1] + SignExtended(offset)][15:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3]);
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "lw":
                    {
                        // lw rd,offset(rs1)
                        // x[rd] = SignExtended(M[x[rs1] + SignExtended(offset)][31:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3]);
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "lbu":
                    {
                        // lbu rd,offset(rs1)
                        // x[rd] = ZeroExtended(M[x[rs1] + SignExtended(offset)][7:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3]);
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "lhu":
                    {
                        // lhu rd,offset(rs1)
                        // x[rd] = ZeroExtended(M[x[rs1] + SignExtended(offset)][15:0])
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3]);
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "sb":
                    {
                        // sb rs2,offset(rs1)
                        // M[x[rs1] + SignExtended(offset)] = x[rs2][7:0]
                        // NOTE: Store 8-bit, values from the
                        // ```low bits of register rs2 to memory.```
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs2 = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3]);
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 11, 0);
                        return [GetStypeInst(mnem, offset, rs1, rs2)];
                    }
                case "sh":
                    {
                        // sh rs2,offset(rs1)
                        // M[x[rs1] + SignExtended(offset)] = x[rs2][15:0]
                        // NOTE: Store 16-bit, values from the
                        // ```low bits of register rs2 to memory.```
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs2 = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3]);
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 11, 0);
                        return [GetStypeInst(mnem, offset, rs1, rs2)];
                    }
                case "sw":
                    {
                        // sw rs2,offset(rs1)
                        // M[x[rs1] + SignExtended(offset)] = x[rs2][31:0]
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs2 = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        string rs1 = GetRegisterIndex(ts[3]);
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 11, 0);
                        return [GetStypeInst(mnem, offset, rs1, rs2)];
                    }
                case "jal":
                    {
                        // jal rd,offset
                        // x[rd] = pc+4; pc += SignExtended(offset) // this is an offset which is added to the pc not the final address
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rd = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 20, 1); // offset = offset[20:1]
                        // TODO: what is this?? -> [20|10:1|11|19:12] in the immediate index for the jal instruction format
                        //offset = string.Concat(offset[0], offset[10..20], offset[9], offset[1..9]); // offset = offset[20|10:1|11|19:12]
                        return [GetUtypeInst(mnem, offset, rd)];
                    }
                case "call":
                    {
                        // call offset
                        // pc += SignExtended(offset)
                        // call offset -> jal ra,offset
                        CheckTokensCount(mnem, ts.Count, 2);
                        string offset = ts[1];
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 20, 1); // offset = offset[20:1]
                        // TODO: what is this?? -> [20|10:1|11|19:12] in the immediate index for the jal instruction format
                        //offset = string.Concat(offset[0], offset[10..20], offset[9], offset[1..9]); // offset = offset[20|10:1|11|19:12]
                        return [GetUtypeInst("jal", offset, GetRegisterIndex("ra"))];
                    }
                case "j":
                    {
                        // j label
                        // pc = label === pc = pc + (label - pc) = pc + (offset)
                        // j offset -> jal x0,offset
                        CheckTokensCount(mnem, ts.Count, 2);
                        string offset = ts[1];
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 20, 1); // offset = offset[20:1]
                        // TODO: what is this?? -> [20|10:1|11|19:12] in the immediate index for the jal instruction format
                        //offset = string.Concat(offset[0], offset[10..20], offset[9], offset[1..9]); // offset = offset[20|10:1|11|19:12]
                        return [GetUtypeInst("jal", offset, GetRegisterIndex("zero"))];
                    }
                case "jalr":
                    {
                        // jalr rd,rs1,offset
                        // t = pc+4;
                        // pc = (x[rs1] + SignExtended(offset)) & ~1;
                        // x[rd] = t
                        // NOTE: the steps above are important and should be implemented exactly as shown
                        // ofcourse they are gonna be executed in parallel in hardware but should be taken into account when performing the operation
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rd = GetRegisterIndex(ts[1]);
                        string rs1 = GetRegisterIndex(ts[2]);
                        string offset = ts[3];
                        offset = LibUtils.GetFromIndexLittle(GetImmediateToBin(offset), 11, 0);
                        return [GetItypeInst(mnem, offset, rs1, rd)];
                    }
                case "ret":
                    {
                        // ret
                        // pc = ra
                        CheckTokensCount(mnem, ts.Count, 1);
                        return [GetItypeInst("jalr", "".PadLeft(12, '0'), GetRegisterIndex("ra"), GetRegisterIndex("zero"))];
                    }
                case "jr":
                    {
                        // jr rs1
                        // pc = x[rs1]
                        // jr rs1 -> jalr x0,rs1,0
                        CheckTokensCount(mnem, ts.Count, 2);
                        string rs1 = GetRegisterIndex(ts[1]);
                        return [GetItypeInst("jalr", "".PadLeft(12, '0'), rs1, GetRegisterIndex("zero"))];
                    }
                case "beq":
                    {
                        // beq rs1,rs2,offset
                        // if (rs1 == rs2) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs1 = GetRegisterIndex(ts[1]);
                        string rs2 = GetRegisterIndex(ts[2]);
                        string offset = ts[3];
                        offset = GetImmediateToBin(offset).Substring(31 - 12, 12);
                        // TODO: see the immediate index for all the branch instruction formats
                        return [GetStypeInst(mnem, offset, rs1, rs2)];
                    }
                case "bne":
                    {
                        // bne rs1,rs2,offset
                        // if (rs1 != rs2) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs1 = GetRegisterIndex(ts[1]);
                        string rs2 = GetRegisterIndex(ts[2]);
                        string offset = ts[3];
                        offset = GetImmediateToBin(offset).Substring(31 - 12, 12);
                        return [GetStypeInst(mnem, offset, rs1, rs2)];
                    }
                case "blt":
                    {
                        // blt rs1,rs2,offset
                        // if (rs1 <s rs2) pc += SignExtended(offset)
                        // if (signed(rs1) <s signed(rs2)) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs1 = GetRegisterIndex(ts[1]);
                        string rs2 = GetRegisterIndex(ts[2]);
                        string offset = ts[3];
                        offset = GetImmediateToBin(offset).Substring(31 - 12, 12);
                        return [GetStypeInst(mnem, offset, rs1, rs2)];
                    }
                case "bltz":
                    {
                        // bltz rs1,offset
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rs1 = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        offset = GetImmediateToBin(offset).Substring(31 - 12, 12);
                        return [GetStypeInst("blt", offset, rs1, GetRegisterIndex("zero"))];
                    }
                case "bge":
                    {
                        // bge rs1,rs2,offset
                        // if (rs1 >=s rs2) pc += SignExtended(offset)
                        // if (signed(rs1) >=s signed(rs2)) pc += SignExtended(offset)
                        CheckTokensCount(mnem, ts.Count, 4);
                        string rs1 = GetRegisterIndex(ts[1]);
                        string rs2 = GetRegisterIndex(ts[2]);
                        string offset = ts[3];
                        offset = GetImmediateToBin(offset).Substring(31 - 12, 12);
                        return [GetStypeInst(mnem, offset, rs1, rs2)];
                    }
                case "bgez":
                    {
                        // bgez rs1,offset
                        CheckTokensCount(mnem, ts.Count, 3);
                        string rs1 = GetRegisterIndex(ts[1]);
                        string offset = ts[2];
                        offset = GetImmediateToBin(offset).Substring(31 - 12, 12);
                        return [GetStypeInst("bge", offset, rs1, GetRegisterIndex("zero"))];
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
        //    else if (mnem == "ret")
        //    {
        //        return [
        //            new([new("jr"), new("ra")])
        //        ];
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

        static (List<(int, string)>, List<(int, string)>) GetTextDataSections(List<(int, string)> src)
        {
            int TextIndex = src.FindIndex(x => x.Item2 == ".section .text");
            int DataIndex = src.FindIndex(x => x.Item2 == ".section .data");

            List<(int, string)> DataSection = [];
            List<(int, string)> TextSection = [];

            if (TextIndex == -1)
                Shartilities.Log(Shartilities.LogType.ERROR, $"text directive doesn't exist\n", 1);

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
                DataSection.RemoveAt(0); // remove: `.section .data`
            }

            TextSection.RemoveAt(0); // remove: `.section .text`
            if (TextSection.Count > 1 && TextSection[0].Item2 == ".globl main" && TextSection[1].Item2 == "main:")
                TextSection.RemoveAt(0);
            else
                Shartilities.Log(Shartilities.LogType.ERROR, $"main is not defined in the assembly program\n", 1);

            return (TextSection, DataSection);
        }
        static string ParseLabel(string input)
        {
            string label = input.Trim();
            int ColonIndex = label.IndexOf(':');
            if (ColonIndex == label.Length - 1)
            {
                label = label[..^1].Trim();
                if (label.Split(' ').Length > 1)
                {
                    Shartilities.Log(Shartilities.LogType.ERROR, $"invalid label syntax `{input}`\n", 1);
                }
                else
                {
                    return label;
                }
            }
            else
            {
                Shartilities.Log(Shartilities.LogType.ERROR, $"the syntax is not supported yet\n");
            }
            Shartilities.UNREACHABLE("ParseLabel");
            return "";
        }
        static uint GetInstructionSize(string mnem)
        {
            return mnem switch
            {
                "la" => 8,
                _ => 4,
            };
        }
        public static Program AssembleProgram(string FilePath)
        {
            string src = File.ReadAllText(FilePath);

            List<string> splitted = [.. src.Split('\n')];
            List<int> lines = [.. Enumerable.Range(1, splitted.Count)];
            List<(int, string)> code = [.. lines.Zip(splitted)];

            for (int i = 0; i < code.Count; i++)
            {
                int index = code[i].Item2.IndexOf('#');
                if (index != -1)
                    code[i] = (code[i].Item1, code[i].Item2[..index]);
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

                if (instruction.Contains(':'))
                {
                    Labels.Add(ParseLabel(instruction), LabelAddress);
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
            Dictionary<string, UInt32> DataSectionAddresses = [];
            UInt32 CurrentDataAddress = 0;
            for (int i = 0; i < DataSection.Count; i++)
            {
                string line = DataSection[i].Item2;
                if (line.Contains(':'))
                {
                    string label = ParseLabel(DataSection[i].Item2);
                    DataSectionAddresses.Add(label, CurrentDataAddress);
                }
                else
                {
                    int index = line.IndexOf(' ');
                    if (index == -1)
                        Shartilities.Log(Shartilities.LogType.ERROR, $"invalid syntax in data section\n", 1);

                    string Directive = line[..index];
                    if (Directive == ".space")
                    {
                        List<string> data = [.. line[index..].Trim().Split(',')];
                        for (int j = 0; j < data.Count; j++) data[j] = data[j].Trim();
                        p.DataMemoryValues.Add(Directive);
                        p.DataMemoryValues.AddRange(data);
                        if (data.Count == 0)
                            Shartilities.Log(Shartilities.LogType.ERROR, $"no expression was provided for .space directive\n", 1);
                        if (data.Count != 1)
                            Shartilities.Log(Shartilities.LogType.ERROR, $"invalid number of expressions should be one for .space directive\n", 1);

                        if (!UInt32.TryParse(data[0], out UInt32 value))
                            Shartilities.Log(Shartilities.LogType.ERROR, $"invalid expression in directive .spcace with the value `{data[0]}`\n", 1);
                        CurrentDataAddress += 1 * value;
                    }
                    else if (Directive == ".string")
                    {
                        string StringLit = line[index..].Trim();
                        if (StringLit.Length > 1 && StringLit[0] == '"' && StringLit[^1] == '"')
                        {
                            p.DataMemoryValues.Add(Directive);
                            i = 1;
                            int len = 0;
                            List<string> temp = [];
                            while (i < StringLit.Length - 1)
                            {
                                char c = StringLit[i];
                                if (c == '\\')
                                {
                                    if (i + 1 < StringLit.Length - 1)
                                    {
                                        if (StringLit[i + 1] == 'n')
                                        {
                                            temp.Add("\n");
                                            i++;
                                            i++;
                                        }
                                    }
                                    else
                                    {
                                        Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported escape character in string literal `{StringLit}`\n", 1);
                                    }
                                }
                                else
                                {
                                    temp.Add($"{c}");
                                    i++;
                                }
                                len++;
                            }
                            p.DataMemoryValues.Add(len.ToString());
                            p.DataMemoryValues.AddRange(temp);
                        }
                        else
                        {
                            Shartilities.Log(Shartilities.LogType.ERROR, $"invalid string literal declaration: `{StringLit}`\n", 1);
                        }
                    }
                    else if (Directive == ".word")
                    {
                        List<string> data = [.. line[index..].Trim().Split(',')];
                        for (int j = 0; j < data.Count; j++) data[j] = data[j].Trim();
                        p.DataMemoryValues.Add(Directive);
                        p.DataMemoryValues.AddRange(data);
                        int size = 4;
                        CurrentDataAddress += (UInt32)(size * data.Count);
                    }
                    else
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported data memory directive `{Directive}`\n", 1);
                    }
                }
            }
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            foreach (KeyValuePair<string, UInt32> address in DataSectionAddresses)
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
                for (int i = 0; i < p.Instructions.Count; i++)
                {
                    for (int j = 1; j < p.Instructions[i].m_tokens.Count; j++)
                    {
                        if (p.Instructions[i].m_tokens[j] == l.Key)
                        {
                            uint offset = l.Value - 4 * (uint)(i);
                            p.Instructions[i].m_tokens[j] = offset.ToString();
                        }
                    }
                }
            }
            /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            for (int i = 0; i < p.Instructions.Count; i++)
            {
                p.MachineCodes.AddRange(Instruction2MachineCodes(p.Instructions[i]));
            }
            // TODO:
            //      - curr_insts = GetInstsAsText(m_prog);
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
