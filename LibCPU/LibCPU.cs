using System;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
namespace LibCPU
{
    public enum CPU_type
    {
        SingleCycle,
    }
    public static class SingleCycle
    {
        static List<string> DataMemory = [];
        static List<int> RegisterFile = [];
        static List<string> InstructionMemory = [];
        const int MAX_CLOCKS = 100 * 1000 * 1000;
        static string nop = "".PadLeft(32, '0');
        static int PC = 0;
        static int CyclesConsumed = 0;

        static void namingrtype(string mc)
        {
            string Funct3 = mc.Substring(31 - 14, 3);
            string Funct7 = mc.Substring(0, 7);
            switch (Funct3)
            {
                case "000":
                    {
                        switch (Funct7)
                        {
                            case "0000000":
                                {
                                    Shartilities.TODO("add");
                                    break;
                                }
                            case "0110000":
                                {
                                    Shartilities.TODO("sub");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 `{Funct7}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "001":
                    {
                        switch (Funct7)
                        {
                            case "0000000":
                                {
                                    Shartilities.TODO("sll");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 `{Funct7}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "010":
                    {
                        switch (Funct7)
                        {
                            case "0000000":
                                {
                                    Shartilities.TODO("slt");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 `{Funct7}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "011":
                    {
                        switch (Funct7)
                        {
                            case "0000000":
                                {
                                    Shartilities.TODO("sltu");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 `{Funct7}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "100":
                    {
                        switch (Funct7)
                        {
                            case "0000000":
                                {
                                    Shartilities.TODO("xor");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 `{Funct7}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "101":
                    {
                        switch (Funct7)
                        {
                            case "0000000":
                                {
                                    Shartilities.TODO("srl");
                                    break;
                                }
                            case "0100000":
                                {
                                    Shartilities.TODO("sra");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 `{Funct7}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "110":
                    {
                        switch (Funct7)
                        {
                            case "0000000":
                                {
                                    Shartilities.TODO("or");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 `{Funct7}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "111":
                    {
                        switch (Funct7)
                        {
                            case "0000000":
                                {
                                    Shartilities.TODO("and");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7 `{Funct7}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 `{Funct3}`\n", 1);
                        break;
                    }
            }
        }
        static void namingitype(string mc)
        {
            string Funct3 = mc.Substring(31 - 14, 3);
            string Funct7Like = mc.Substring(0, 7);
            switch (Funct3)
            {
                case "000":
                    {
                        Shartilities.TODO("addi");
                        break;
                    }
                case "010":
                    {
                        Shartilities.TODO("slti");
                        break;
                    }
                case "011":
                    {
                        Shartilities.TODO("sltiu");
                        break;
                    }
                case "100":
                    {
                        Shartilities.TODO("xori");
                        break;
                    }
                case "110":
                    {
                        Shartilities.TODO("ori");
                        break;
                    }
                case "111":
                    {
                        Shartilities.TODO("andi");
                        break;
                    }
                case "001":
                    {
                        Shartilities.TODO("slli");
                        break;
                    }
                case "101":
                    {
                        switch (Funct7Like)
                        {
                            case "0000000":
                                {
                                    Shartilities.TODO("srli");
                                    break;
                                }
                            case "0100000":
                                {
                                    Shartilities.TODO("srai");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct7-like `{Funct7Like}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                default:
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 `{Funct3}`\n", 1);
                        break;
                    }
            }
        }
        static void ConsumeInstruction(string mc)
        {
            string opcode = mc.Substring(31 - 6, 7);
            string Funct3 = mc.Substring(31 - 14, 3);
            switch (opcode)
            {
                case "0110011":
                    {
                        namingrtype(mc);
                        break;
                    }
                case "0010011":
                    {
                        namingitype(mc);
                        break;
                    }
                case "1110011":
                    {
                        Shartilities.TODO("ecall");
                        break;
                    }
                case "0000011":
                    {
                        switch (Funct3)
                        {
                            case "000":
                                {
                                    Shartilities.TODO("lb");
                                    break;
                                }
                            case "001":
                                {
                                    Shartilities.TODO("lh");
                                    break;
                                }
                            case "010":
                                {
                                    Shartilities.TODO("lw");
                                    break;
                                }
                            case "100":
                                {
                                    Shartilities.TODO("lbu");
                                    break;
                                }
                            case "101":
                                {
                                    Shartilities.TODO("lhu");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 `{Funct3}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "1110111":
                    {
                        Shartilities.TODO("jalr");
                        break;
                    }
                case "0100011":
                    {
                        switch (Funct3)
                        {
                            case "000":
                                {
                                    Shartilities.TODO("sb");
                                    break;
                                }
                            case "001":
                                {
                                    Shartilities.TODO("sh");
                                    break;
                                }
                            case "010":
                                {
                                    Shartilities.TODO("sw");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 `{Funct3}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "1100011":
                    {
                        switch (Funct3)
                        {
                            case "000":
                                {
                                    Shartilities.TODO("beq");
                                    break;
                                }
                            case "001":
                                {
                                    Shartilities.TODO("bne");
                                    break;
                                }
                            case "100":
                                {
                                    Shartilities.TODO("blt");
                                    break;
                                }
                            case "101":
                                {
                                    Shartilities.TODO("bge");
                                    break;
                                }
                            default:
                                {
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 `{Funct3}`\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                case "0110111":
                    {
                        Shartilities.TODO("lui");
                        break;
                    }
                case "0010111":
                    {
                        Shartilities.TODO("auipc");
                        break;
                    }
                case "1111111":
                    {
                        Shartilities.TODO("jal");
                        break;
                    }
                default:
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"unsuppored opcode `{opcode}`\n", 1);
                        Shartilities.UNREACHABLE("invalid opcode");
                        break;
                    }
            }
        }
        public static (int, List<int>, List<string>) Run(List<string> MachingCodes, List<string> DataMemoryInit, uint IM_SIZE, uint DM_SIZE)
        {
            nop = "".PadLeft(32, '0');
            PC = 0;
            CyclesConsumed = 0;

            InstructionMemory = [];
            InstructionMemory.AddRange(MachingCodes);
            int imcount = InstructionMemory.Count;
            for (int i = 0; i < IM_SIZE - imcount; i++) InstructionMemory.Add(nop);

            RegisterFile = [];
            for (int i = 0; i < 32; i++) RegisterFile.Add(0);

            DataMemory = [];
            DataMemory.AddRange(DataMemoryInit);
            int dmcount = DataMemory.Count;
            for (int i = 0; i < DM_SIZE - dmcount; i++) DataMemory.Add("0");

            while (CyclesConsumed < MAX_CLOCKS)
            {
                string mc = MachingCodes[PC];
                ConsumeInstruction(mc);
                CyclesConsumed++;
            }
            if (CyclesConsumed == MAX_CLOCKS)
            {
                Shartilities.Log(Shartilities.LogType.ERROR, $"cycles consumed reached the limit\n", 1);
            }
            return (CyclesConsumed, RegisterFile, DataMemory);
        }
    }
}
