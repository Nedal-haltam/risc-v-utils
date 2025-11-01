using System;
using System.Data;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using System.Transactions;
using static LibUtils;

namespace LibCPU
{
    public enum CPU_type
    {
        SingleCycle,
    }
    public struct RegisterFile
    {
        // list of DoubleWords in base-2
        public List<string> Registers;
        public RegisterFile()
        {
            Registers = [];
            string zero = zext("", 64);
            for (int i = 0; i < 32; i++) Registers.Add(zero);
        }
        public readonly long this[int index]
        {
            get
            {
                Shartilities.Assert(0 <= index && index < Registers.Count, $"index out of bound in register file\n");
                return Convert.ToInt64(Registers[index], 2);
            }
            set
            {
                Shartilities.Assert(0 <= index && index < Registers.Count, $"index out of bound in register file\n");
                if (index != 0) 
                    Registers[index] = zext(Convert.ToString(value, 2), 64);
            }
        }
    }
    public readonly struct Memory
    {
        // list of byte in base-2
        private readonly List<string> m_Memory;
        private readonly uint m_DM_SIZE;
        public Memory(List<string> DataMemoryInit, uint DM_SIZE, out int n)
        {
            m_DM_SIZE = DM_SIZE;
            m_Memory = [];
            foreach (string s in DataMemoryInit)
            {
                try
                {
                    m_Memory.Add(zext(Convert.ToString(Convert.ToByte(s, 10), 2), 8));
                }
                catch
                {
                    Shartilities.Log(Shartilities.LogType.ERROR, $"invalid data memory initialization with value {s}\n", 1);
                }
            }
            int dmcount = m_Memory.Count;
            string zero = zext("", 8);
            for (uint i = 0; i < DM_SIZE - dmcount; i++) m_Memory.Add(zero);
            n = dmcount;
        }
        public readonly void Clear()
        {
            m_Memory.Clear();
            for (int i = 0; i < m_DM_SIZE; i++) m_Memory.Add("0");
        }
        public readonly List<string> GetMemory() => m_Memory;
        public readonly string GetByte(int Address)
        {
            return m_Memory[Address];
        }
        public readonly string GetHalfWord(int Address)
        {
            string ret = "";
            for (int i = 0; i < 2; i++)
                ret = m_Memory[Address + i] + ret;
            return ret;
        }
        public readonly string GetWord(int Address)
        {
            string ret = "";
            for (int i = 0; i < 4; i++)
                ret = m_Memory[Address + i] + ret;

            return ret;
        }
        public readonly string GetDoubleWord(int Address)
        {
            string ret = "";
            for (int i = 0; i < 8; i++)
                ret = m_Memory[Address + i] + ret;
            return ret;
        }
        public readonly void SetByte(int Address, string BinaryByte)
        {
            m_Memory[Address] = GetFromIndexLittle(BinaryByte, 7, 0);
        }
        public readonly void SetHalfWord(int Address, string BinaryHalfWord)
        {
            for (int i = 0; i < 2; i++)
                m_Memory[Address + i] = GetFromIndexLittle(BinaryHalfWord, 8 * (i + 1) - 1, 8 * i);
        }
        public readonly void SetWord(int Address, string BinaryWord)
        {
            for (int i = 0; i < 4; i++)
                m_Memory[Address + i] = GetFromIndexLittle(BinaryWord, 8 * (i + 1) - 1, 8 * i);
        }
        public readonly void SetDoubleWord(int Address, string BinaryDoubleWord)
        {
            for (int i = 0; i < 8; i++)
                m_Memory[Address + i] = GetFromIndexLittle(BinaryDoubleWord, 8 * (i + 1) - 1, 8 * i);
        }
    }
    public static class SingleCycle
    {
        ///////////////////////////////////////////////
        const int MAX_CLOCKS = 1000 * 1000 * 1000;
        static List<string> InstructionMemory = [];
        static RegisterFile RegisterFile;
        static Memory DataMemory;

        static string? OutputFilePath;
        static long PC = 0;
        static int CyclesConsumed = 0;
        ///////////////////////////////////////////////
        static (int rs1, int rs2, int rd) GetRtypeInst(string mc)
        {
            return 
            (
                (int)Convert.ToUInt32(GetFromIndexLittle(mc, 19, 15), 2),
                (int)Convert.ToUInt32(GetFromIndexLittle(mc, 24, 20), 2),
                (int)Convert.ToUInt32(GetFromIndexLittle(mc, 11, 7) , 2) 
            );
        }
        static (int rs1, string imm12, int rd) GetItypeInst(string mc)
        {
            return
            (
                (int)Convert.ToUInt32(GetFromIndexLittle(mc, 19, 15), 2),
                GetFromIndexLittle(mc, 31, 20),
                (int)Convert.ToUInt32(GetFromIndexLittle(mc, 11, 7), 2)  
            );
        }
        static (int rs1, int rs2, string imm12) GetStypeInst(string mc)
        {
            return
            (
                (int)Convert.ToUInt32(GetFromIndexLittle(mc, 19, 15), 2),
                (int)Convert.ToUInt32(GetFromIndexLittle(mc, 24, 20), 2),
                GetFromIndexLittle(mc, 31, 25) + GetFromIndexLittle(mc, 11, 7)
            );
        }
        static (int rd, string imm20) GetUtypeInst(string mc)
        {
            return
            (
                (int)Convert.ToUInt32(GetFromIndexLittle(mc, 11, 7), 2), 
                GetFromIndexLittle(mc, 31, 12)
            );
        }

        static void ConsumeRType(string mc, int rs1, int rs2, int rd)
        {
            string Funct3 = GetFromIndexLittle(mc, 14, 12);
            string Funct7 = GetFromIndexLittle(mc, 31, 25);
            switch (Funct3)
            {
                case "000":
                    {
                        switch (Funct7)
                        {
                            case "0000000": // "add"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] + RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            case "0110000": // "sub"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] - RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            case "0000001": // "mul"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] * RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 {Funct7}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "001":
                    {
                        switch (Funct7)
                        {
                            case "0000000": // "sll"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] << ((int)(RegisterFile[rs2] & 0x3F));
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 {Funct7}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "010":
                    {
                        switch (Funct7)
                        {
                            case "0000000": // "slt"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] < RegisterFile[rs2] ? 1 : 0;
                                    PC += 4;
                                    break;
                                }
                            case "0000001": // "seq"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] == RegisterFile[rs2] ? 1 : 0;
                                    PC += 4;
                                    break;
                                }
                            case "0000010": // "sne"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] != RegisterFile[rs2] ? 1 : 0;
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 {Funct7}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "011":
                    {
                        switch (Funct7)
                        {
                            case "0000000": // "sltu"
                                {
                                    RegisterFile[rd] = (ulong)RegisterFile[rs1] < (ulong)RegisterFile[rs2] ? 1 : 0;
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 {Funct7}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "100":
                    {
                        switch (Funct7)
                        {
                            case "0000000": // "xor"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] ^ RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            case "0000001": // "div"
                                {
                                    if (RegisterFile[rs2] == 0)
                                        Shartilities.Log(Shartilities.LogType.ERROR, $"divide by zero exception\n", 1);
                                    RegisterFile[rd] = RegisterFile[rs1] / RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 {Funct7}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "101":
                    {
                        switch (Funct7)
                        {
                            case "0000000": // "srl"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] >>> ((int)(RegisterFile[rs2] & 0x3F));
                                    PC += 4;
                                    break;
                                }
                            case "0100000": // "sra"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] >> ((int)(RegisterFile[rs2] & 0x3F));
                                    PC += 4;
                                    break;
                                }
                            case "0000001": // "divu"
                                {
                                    if (RegisterFile[rs2] == 0)
                                        Shartilities.Log(Shartilities.LogType.ERROR, $"divide by zero exception\n", 1);
                                    RegisterFile[rd] = (long)((ulong)RegisterFile[rs1] / (ulong)RegisterFile[rs2]);
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 {Funct7}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "110":
                    {
                        switch (Funct7)
                        {
                            case "0000000": // "or"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] | RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            case "0000001": // "rem"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] % RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 {Funct7}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "111":
                    {
                        switch (Funct7)
                        {
                            case "0000000": // "and"
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] & RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            case "0000001": // "remu"
                                {
                                    RegisterFile[rd] = (long)((ulong)RegisterFile[rs1] % (ulong)RegisterFile[rs2]);
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 {Funct7}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 {Funct3}\n", 1);
                        break;
                    }
            }
        }
        static void ConsumeIType(string mc, int rs1, string imm12, int rd)
        {
            string Funct3 = GetFromIndexLittle(mc, 14, 12);
            string Funct7Like = GetFromIndexLittle(mc, 31, 25);
            switch (Funct3)
            {
                case "000": // "addi"
                    {
                        long imm = Convert.ToInt64(sext(imm12, 64), 2);
                        RegisterFile[rd] = RegisterFile[rs1] + imm;
                        PC += 4;
                        break;
                    }
                case "010": // "slti"
                    {
                        long imm = Convert.ToInt64(sext(imm12, 64), 2);
                        RegisterFile[rd] = RegisterFile[rs1] < imm ? 1 : 0;
                        PC += 4;
                        break;
                    }
                case "011": // "sltiu"
                    {
                        long imm = Convert.ToInt64(sext(imm12, 64), 2);
                        RegisterFile[rd] = (ulong)RegisterFile[rs1] < (ulong)imm ? 1 : 0;
                        PC += 4;
                        break;
                    }
                case "100": // "xori"
                    {
                        long imm = Convert.ToInt64(sext(imm12, 64), 2);
                        RegisterFile[rd] = RegisterFile[rs1] ^ imm;
                        PC += 4;
                        break;
                    }
                case "110": // "ori"
                    {
                        long imm = Convert.ToInt64(sext(imm12, 64), 2);
                        RegisterFile[rd] = RegisterFile[rs1] | imm;
                        PC += 4;
                        break;
                    }
                case "111": // "andi"
                    {
                        long imm = Convert.ToInt64(sext(imm12, 64), 2);
                        RegisterFile[rd] = RegisterFile[rs1] & imm;
                        PC += 4;
                        break;
                    }
                case "001": // "slli"
                    {
                        ulong imm = (ulong)Convert.ToInt64(zext(imm12, 64), 2);
                        if (imm > 63)
                            Shartilities.Logln(Shartilities.LogType.ERROR, $"Error: improper shift amount ({imm})", 1);
                        RegisterFile[rd] = RegisterFile[rs1] << (int)imm;
                        PC += 4;
                        break;
                    }
                case "101":
                    {
                        switch (Funct7Like)
                        {
                            case "0000000": // "srli"
                                {
                                    ulong imm = (ulong)Convert.ToInt64(zext(imm12, 64), 2);
                                    if (imm > 63)
                                        Shartilities.Logln(Shartilities.LogType.ERROR, $"Error: improper shift amount ({imm})", 1);
                                    RegisterFile[rd] = RegisterFile[rs1] >>> (int)imm;
                                    PC += 4;
                                    break;
                                }
                            case "0100000": // "srai"
                                {
                                    ulong imm = (ulong)Convert.ToInt64(zext(imm12, 64), 2);
                                    if (imm > 63)
                                        Shartilities.Logln(Shartilities.LogType.ERROR, $"Error: improper shift amount ({imm})", 1);
                                    RegisterFile[rd] = RegisterFile[rs1] >> (int)imm;
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7-like {Funct7Like}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 {Funct3}\n", 1);
                        break;
                    }
            }
        }
        static void ECALL()
        {
            long syscall = RegisterFile[REG_LIST["a7"].Item2];
            if (syscall == 64)
            {
                long FileDescriptor = RegisterFile[REG_LIST["a0"].Item2];
                long StringLitAddress = RegisterFile[REG_LIST["a1"].Item2];
                long StringLitLength = RegisterFile[REG_LIST["a2"].Item2];
                StringBuilder buffer = new();
                if (FileDescriptor == 1)
                {
                    while (StringLitLength-- > 0)
                    {
                        byte b = Convert.ToByte(DataMemory.GetByte((int)StringLitAddress), 2);
                        buffer.Append((char)b);
                        StringLitAddress++;
                    }
                    Console.Write(buffer.ToString());
                }
                else
                {
                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported file descriptor {FileDescriptor}\n", 1);
                }
            }
            else if (syscall == 93)
            {
                int ExitCode = (int)RegisterFile[REG_LIST["a0"].Item2];
                if (OutputFilePath != null)
                    GenerateRegFileDMStates(OutputFilePath);
                if (ExitCode < 0)
                    Shartilities.Log(Shartilities.LogType.ERROR, $"exit code cannot be negative\n", 1);
                Environment.Exit(ExitCode);
            }
            else
            {
                Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported syscall {syscall}\n", 1);
            }
        }
        static void ConsumeInstruction(string mc)
        {
            string opcode = GetFromIndexLittle(mc, 6, 0);
            string Funct3 = GetFromIndexLittle(mc, 14, 12);
            switch (opcode)
            {
                // start of R-TYPE instructions
                case "0110011":
                    {
                        (int rs1, int rs2, int rd) = GetRtypeInst(mc);
                        ConsumeRType(mc, rs1, rs2, rd);
                        break;
                    }
                // end of R-TYPE instructions
                // start of I-TYPE instructions
                case "0010011":
                    {
                        (int rs1, string imm12, int rd) = GetItypeInst(mc);
                        ConsumeIType(mc, rs1, imm12, rd);
                        break;
                    }
                case "1110011":
                    {
                        string Funct12 = GetFromIndexLittle(mc, 31, 20);
                        switch (Funct3)
                        {
                            case "000":
                                {
                                    switch(Funct12)
                                    {
                                        case "000000000000": // "ecall"
                                            {
                                                ECALL();
                                                PC += 4;
                                                break;
                                            }
                                    }
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 {Funct3}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "0000011":
                    {
                        (int rs1, string imm12, int rd) = GetItypeInst(mc);
                        switch (Funct3)
                        {
                            case "000": // "lb"
                                {
                                    long Address = RegisterFile[rs1] + Convert.ToInt64(sext(imm12, 64), 2);
                                    string value = DataMemory.GetByte((int)Address);
                                    RegisterFile[rd] = Convert.ToInt64(sext(value, 64), 2);
                                    PC += 4;
                                    break;
                                }
                            case "001": // "lh"
                                {
                                    long Address = RegisterFile[rs1] + Convert.ToInt64(sext(imm12, 64), 2);
                                    string value = DataMemory.GetHalfWord((int)Address);
                                    RegisterFile[rd] = Convert.ToInt64(sext(value, 64), 2);
                                    PC += 4;
                                    break;
                                }
                            case "010": // "lw"
                                {
                                    long Address = RegisterFile[rs1] + Convert.ToInt64(sext(imm12, 64), 2);
                                    string value = DataMemory.GetWord((int)Address);
                                    RegisterFile[rd] = Convert.ToInt64(sext(value, 64), 2);
                                    PC += 4;
                                    break;
                                }
                            case "011": // "ld"
                                {
                                    long Address = RegisterFile[rs1] + Convert.ToInt64(sext(imm12, 64), 2);
                                    string value = DataMemory.GetDoubleWord((int)Address);
                                    RegisterFile[rd] = Convert.ToInt64(sext(value, 64), 2);
                                    PC += 4;
                                    break;
                                }
                            case "100": // "lbu"
                                {
                                    long Address = RegisterFile[rs1] + Convert.ToInt64(sext(imm12, 64), 2);
                                    string value = DataMemory.GetByte((int)Address);
                                    RegisterFile[rd] = Convert.ToInt64(zext(value, 64), 2);
                                    PC += 4;
                                    break;
                                }
                            case "101": // "lhu"
                                {
                                    long Address = RegisterFile[rs1] + Convert.ToInt64(sext(imm12, 64), 2);
                                    string value = DataMemory.GetHalfWord((int)Address);
                                    RegisterFile[rd] = Convert.ToInt64(zext(value, 64), 2);
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 {Funct3}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "1110111": // "jalr"
                    {
                        switch (Funct3)
                        {
                            case "000":
                                {
                                    (int rs1, string imm12, int rd) = GetItypeInst(mc);
                                    long t = PC + 4;
                                    long imm = Convert.ToInt64(sext(imm12, 64), 2);
                                    PC = (RegisterFile[rs1] + imm) & ~1;
                                    RegisterFile[rd] = t;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 {Funct3}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                // end of I-TYPE instructions
                // start of S-TYPE instructions
                case "0100011":
                    {
                        (int rs1, int rs2, string imm12) = GetStypeInst(mc);
                        switch (Funct3)
                        {
                            case "000": // "sb"
                                {
                                    long Address = RegisterFile[rs1] + Convert.ToInt64(sext(imm12, 64), 2);
                                    string value = GetFromIndexLittle(LongToBin(RegisterFile[rs2]), 7, 0);
                                    DataMemory.SetByte((int)Address, value);
                                    PC += 4;
                                    break;
                                }
                            case "001": // "sh"
                                {
                                    long Address = RegisterFile[rs1] + Convert.ToInt64(sext(imm12, 64), 2);
                                    string value = GetFromIndexLittle(LongToBin(RegisterFile[rs2]), 15, 0);
                                    DataMemory.SetHalfWord((int)Address, value);
                                    PC += 4;
                                    break;
                                }
                            case "010": // "sw"
                                {
                                    long Address = RegisterFile[rs1] + Convert.ToInt64(sext(imm12, 64), 2);
                                    string value = GetFromIndexLittle(LongToBin(RegisterFile[rs2]), 31, 0);
                                    DataMemory.SetWord((int)Address, value);
                                    PC += 4;
                                    break;
                                }
                            case "011": // "sd"
                                {
                                    long Address = RegisterFile[rs1] + Convert.ToInt64(sext(imm12, 64), 2);
                                    string value = GetFromIndexLittle(LongToBin(RegisterFile[rs2]), 63, 0);
                                    DataMemory.SetDoubleWord((int)Address, value);
                                    PC += 4;
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 {Funct3}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "1100011":
                    {
                        (int rs1, int rs2, string imm12) = GetStypeInst(mc);
                        switch (Funct3)
                        {
                            case "000": // "beq"
                                {
                                    long offset = Convert.ToInt64(sext(imm12, 64), 2) << 1;
                                    if (RegisterFile[rs1] == RegisterFile[rs2])
                                    {
                                        PC += offset;
                                    }
                                    else
                                    {
                                        PC += 4;
                                    }
                                    break;
                                }
                            case "001": // "bne"
                                {
                                    long offset = Convert.ToInt64(sext(imm12, 64), 2) << 1;
                                    if (RegisterFile[rs1] != RegisterFile[rs2])
                                    {
                                        PC += offset;
                                    }
                                    else
                                    {
                                        PC += 4;
                                    }
                                    break;
                                }
                            case "100": // "blt"
                                {
                                    long offset = Convert.ToInt64(sext(imm12, 64), 2) << 1;
                                    if (RegisterFile[rs1] < RegisterFile[rs2])
                                    {
                                        PC += offset;
                                    }
                                    else
                                    {
                                        PC += 4;
                                    }
                                    break;
                                }
                            case "101": // "bge"
                                {
                                    long offset = Convert.ToInt64(sext(imm12, 64), 2) << 1;
                                    if (RegisterFile[rs1] >= RegisterFile[rs2])
                                    {
                                        PC += offset;
                                    }
                                    else
                                    {
                                        PC += 4;
                                    }
                                    break;
                                }
                            case "110": // "bltu"
                                {
                                    long offset = Convert.ToInt64(sext(imm12, 64), 2) << 1;
                                    if ((ulong)RegisterFile[rs1] < (ulong)RegisterFile[rs2])
                                    {
                                        PC += offset;
                                    }
                                    else
                                    {
                                        PC += 4;
                                    }
                                    break;
                                }
                            case "111": // "bgeu"
                                {
                                    long offset = Convert.ToInt64(sext(imm12, 64), 2) << 1;
                                    if ((ulong)RegisterFile[rs1] >= (ulong)RegisterFile[rs2])
                                    {
                                        PC += offset;
                                    }
                                    else
                                    {
                                        PC += 4;
                                    }
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 {Funct3}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                // end of S-TYPE instructions
                // start of U-TYPE instructions
                case "0110111": // "lui"
                    {
                        (int rd, string imm20) = GetUtypeInst(mc);
                        imm20 += zext("", 12);
                        long imm = Convert.ToInt64(sext(imm20, 64), 2);
                        RegisterFile[rd] = imm;
                        PC += 4;
                        break;
                    }
                case "0010111": // "auipc"
                    {
                        (int rd, string imm20) = GetUtypeInst(mc);
                        imm20 += zext("", 12);
                        long imm = Convert.ToInt64(sext(imm20, 64), 2);
                        RegisterFile[rd] = PC + imm;
                        PC += 4;
                        break;
                    }
                case "1111111": // "jal"
                    {
                        (int rd, string imm20) = GetUtypeInst(mc);
                        long imm = Convert.ToInt64(sext(imm20, 64), 2) << 1;
                        RegisterFile[rd] = PC + 4;
                        PC += imm;
                        break;
                    }
                case "1111110": // "addi20u"
                    {
                        (int rd, string imm20) = GetUtypeInst(mc);
                        long imm = Convert.ToInt64(zext(imm20, 64), 2);
                        RegisterFile[rd] = RegisterFile[rd] + imm;
                        PC += 4;
                        break;
                    }
                default:
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"unsuppored opcode {opcode}, mc = `{mc}`\n", 1);
                        Shartilities.UNREACHABLE("invalid opcode");
                        break;
                    }
                // end of U-TYPE instructions
            }
        }
        public static void Run(List<string> MachingCodes, List<string> DataMemoryInit, uint IM_SIZE, uint DM_SIZE, List<string> ClArgs, string? inputOutputFilePath = null)
        {
            PC = 0;
            CyclesConsumed = 0;
            OutputFilePath = inputOutputFilePath;
            InstructionMemory = [];
            RegisterFile = new();
            RegisterFile[REG_LIST["sp"].Item2] = DM_SIZE;
            DataMemory = new(DataMemoryInit, DM_SIZE, out int n);
            n++;

            List<long> ClArgsAddresses = [];
            for (int i = 0; i < ClArgs.Count; i++)
            {
                string arg = ClArgs[i];
                ClArgsAddresses.Add(n);
                foreach (char c in arg)
                    DataMemory.SetByte(n++, zext(Convert.ToString((byte)c, 2), 8));
                DataMemory.SetByte(n++, zext("", 8));
            }

            long AddressOfPointer = n;
            foreach (long addr in ClArgsAddresses)
            {
                DataMemory.SetDoubleWord(n, zext(Convert.ToString(addr, 2), 64));
                n += 8;
            }

            RegisterFile[REG_LIST["a0"].Item2] = ClArgs.Count;
            RegisterFile[REG_LIST["a1"].Item2] = AddressOfPointer;

            foreach (string code in MachingCodes)
            {
                InstructionMemory.Add(GetFromIndexLittle(code, 1 * 8 - 1, 0 * 8));
                InstructionMemory.Add(GetFromIndexLittle(code, 2 * 8 - 1, 1 * 8));
                InstructionMemory.Add(GetFromIndexLittle(code, 3 * 8 - 1, 2 * 8));
                InstructionMemory.Add(GetFromIndexLittle(code, 4 * 8 - 1, 3 * 8));
            }
            int imcount = InstructionMemory.Count;
            for (int i = 0; i < IM_SIZE - imcount; i++)
                InstructionMemory.Add("00000000");
            
            while (CyclesConsumed < MAX_CLOCKS)
            {
                CyclesConsumed++;
                string mc = InstructionMemory[(int)PC + 3] + InstructionMemory[(int)PC + 2] + InstructionMemory[(int)PC + 1] + InstructionMemory[(int)PC];
                ConsumeInstruction(mc);
            }
            if (CyclesConsumed == MAX_CLOCKS)
            {
                Shartilities.Log(Shartilities.LogType.ERROR, $"cycles consumed reached the limit, it consumed: {MAX_CLOCKS}\n", 1);
            }
        }
        static void GenerateRegFileDMStates(string OutPutFilePath)
        {
            if (OutPutFilePath != null)
            {
                StringBuilder sb = new();
                sb.Append($"Number of cycles consumed : {CyclesConsumed,10}\n");
                sb.Append(GetRegs(RegisterFile.Registers));
                sb.Append(GetDM(DataMemory.GetMemory()));
                File.WriteAllText(OutPutFilePath, sb.ToString());
                Shartilities.Log(Shartilities.LogType.INFO, $"Generated CAS output of singlecyle in path {OutPutFilePath} successfully\n");
            }
        }
    }
}
