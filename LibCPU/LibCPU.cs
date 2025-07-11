using System.Text;

namespace LibCPU
{
    public enum CPU_type
    {
        SingleCycle,
    }
    public struct RegisterFile
    {
        public List<string> Registers;
        public RegisterFile()
        {
            Registers = [];
            for (int i = 0; i < 32; i++) Registers.Add("0");
        }
        public Int64 this[int index]
        {
            get
            {
                Shartilities.Assert(0 <= index && index < Registers.Count, $"index out of bound in register file\n");
                return Convert.ToInt64(Registers[index], 10);
            }
            set
            {
                Shartilities.Assert(0 <= index && index < Registers.Count, $"index out of bound in register file\n");
                if (index != 0)
                    Registers[index] = value.ToString();
            }
        }
    }
    public struct Memory
    {
        // list of byte in base-10
        public List<string> memory;
        public Memory(List<string> DataMemoryInit, uint DM_SIZE)
        {
            memory = [];
            memory.AddRange(DataMemoryInit);
            int dmcount = memory.Count;
            for (int i = 0; i < DM_SIZE - dmcount; i++) memory.Add("0");
        }
        public byte GetByte(int Address)
        {
            Shartilities.TODO("GetByte");
            return 0;
        }
        public byte GetHalfWord(int Address)
        {
            Shartilities.TODO("GetHalfWord");
            return 0;
        }
        public byte GetWord(int Address)
        {
            Shartilities.TODO("GetWord");
            return 0;
        }
        public byte GetDouble(int Address)
        {
            Shartilities.TODO("GetDouble");
            return 0;
        }
    }
    public static class SingleCycle
    {
        const int MAX_CLOCKS = 100 * 1000 * 1000;
        static List<string> InstructionMemory = [];
        static RegisterFile RegisterFile;
        static Memory DataMemory;

        static string? OutputFilePath;
        static Int64 PC = 0;
        static int CyclesConsumed = 0;

        static (int rs1, int rs2, int rd) GetRtypeInst(string mc)
        {
            return 
            (
                (int)Convert.ToUInt32(LibUtils.GetFromIndexLittle(mc, 19, 15), 2),
                (int)Convert.ToUInt32(LibUtils.GetFromIndexLittle(mc, 24, 20), 2),
                (int)Convert.ToUInt32(LibUtils.GetFromIndexLittle(mc, 11, 7) , 2) 
            );
        }
        static (int rs1, string imm12, int rd) GetItypeInst(string mc)
        {
            return
            (
                (int)Convert.ToUInt32(LibUtils.GetFromIndexLittle(mc, 19, 15), 2),
                LibUtils.GetFromIndexLittle(mc, 31, 20),
                (int)Convert.ToUInt32(LibUtils.GetFromIndexLittle(mc, 11, 7), 2)  
            );
        }
        static (int rs1, int rs2, string imm12) GetStypeInst(string mc)
        {
            return
            (
                (int)Convert.ToUInt32(LibUtils.GetFromIndexLittle(mc, 19, 15), 2),
                (int)Convert.ToUInt32(LibUtils.GetFromIndexLittle(mc, 24, 20), 2),
                LibUtils.GetFromIndexLittle(mc, 31, 25) + LibUtils.GetFromIndexLittle(mc, 11, 7)
            );
        }
        static (int rd, string imm20) GetUtypeInst(string mc)
        {
            return
            (
                (int)Convert.ToUInt32(LibUtils.GetFromIndexLittle(mc, 11, 7), 2), 
                LibUtils.GetFromIndexLittle(mc, 31, 12)
            );
        }

        static void ConsumeRType(string mc, int rs1, int rs2, int rd)
        {
            string Funct3 = LibUtils.GetFromIndexLittle(mc, 14, 12);
            string Funct7 = LibUtils.GetFromIndexLittle(mc, 31, 25);
            switch (Funct3)
            {
                case "000":
                    {
                        switch (Funct7)
                        {
                            case "0000000":
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] + RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            case "0110000":
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] - RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            case "0000001":
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
                            case "0000000":
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] << ((int)RegisterFile[rs2] & 0x1F);
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
                            case "0000000":
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] < RegisterFile[rs2] ? 1 : 0;
                                    PC += 4;
                                    break;
                                }
                            case "0000001":
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] == RegisterFile[rs2] ? 1 : 0;
                                    PC += 4;
                                    break;
                                }
                            case "0000010":
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
                            case "0000000":
                                {
                                    RegisterFile[rd] = (UInt64)RegisterFile[rs1] < (UInt64)RegisterFile[rs2] ? 1 : 0;
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
                            case "0000000":
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] ^ RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            case "0000001":
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
                            case "0000000":
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] >>> ((int)RegisterFile[rs2] & 0x1F);
                                    PC += 4;
                                    break;
                                }
                            case "0100000":
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] >> ((int)RegisterFile[rs2] & 0x1F);
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
                            case "0000000":
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] | RegisterFile[rs2];
                                    PC += 4;
                                    break;
                                }
                            case "0000001":
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
                            case "0000000":
                                {
                                    RegisterFile[rd] = RegisterFile[rs1] & RegisterFile[rs2];
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
        static void ConsumeIType(string mc, int rs1, string imm12_, int rd)
        {
            string Funct3 = LibUtils.GetFromIndexLittle(mc, 14, 12);
            string Funct7Like = LibUtils.GetFromIndexLittle(mc, 31, 25);
            switch (Funct3)
            {
                case "000":
                    {
                        Int64 imm = Convert.ToInt64(imm12_.PadLeft(64, imm12_[0]), 2);
                        RegisterFile[rd] = RegisterFile[rs1] + imm;
                        PC += 4;
                        break;
                    }
                case "010":
                    {
                        Int64 imm = Convert.ToInt64(imm12_.PadLeft(64, imm12_[0]), 2);
                        RegisterFile[rd] = RegisterFile[rs1] < imm ? 1 : 0;
                        PC += 4;
                        break;
                    }
                case "011":
                    {
                        Int64 imm = Convert.ToInt64(imm12_.PadLeft(64, imm12_[0]), 2);
                        RegisterFile[rd] = (UInt64)RegisterFile[rs1] < (UInt64)imm ? 1 : 0;
                        PC += 4;
                        break;
                    }
                case "100":
                    {
                        Int64 imm = Convert.ToInt64(imm12_.PadLeft(64, imm12_[0]), 2);
                        RegisterFile[rd] = RegisterFile[rs1] ^ imm;
                        PC += 4;
                        break;
                    }
                case "110":
                    {
                        Int64 imm = Convert.ToInt64(imm12_.PadLeft(64, imm12_[0]), 2);
                        RegisterFile[rd] = RegisterFile[rs1] | imm;
                        PC += 4;
                        break;
                    }
                case "111":
                    {
                        Int64 imm = Convert.ToInt64(imm12_.PadLeft(64, imm12_[0]), 2);
                        RegisterFile[rd] = RegisterFile[rs1] & imm;
                        PC += 4;
                        break;
                    }
                case "001":
                    {
                        Int64 imm = Convert.ToInt64(imm12_.PadLeft(64, '0'), 2);
                        RegisterFile[rd] = RegisterFile[rs1] << (int)imm;
                        PC += 4;
                        break;
                    }
                case "101":
                    {
                        switch (Funct7Like)
                        {
                            case "0000000":
                                {
                                    Int64 imm = Convert.ToInt64(imm12_.PadLeft(64, '0'), 2);
                                    RegisterFile[rd] = RegisterFile[rs1] >>> (int)imm;
                                    PC += 4;
                                    break;
                                }
                            case "0100000":
                                {
                                    Int64 imm = Convert.ToInt64(imm12_.PadLeft(64, '0'), 2);
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
        static void ConsumeInstruction(string mc)
        {
            string opcode = LibUtils.GetFromIndexLittle(mc, 6, 0);
            string Funct3 = LibUtils.GetFromIndexLittle(mc, 14, 12);
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
                        Int64 syscall = RegisterFile[LibUtils.REG_LIST["a7"]];
                        if (syscall == 64)
                        {
                            Int64 FileDescriptor = RegisterFile[LibUtils.REG_LIST["a0"]];
                            Int64 StringLitAddress = RegisterFile[LibUtils.REG_LIST["a1"]];
                            Int64 StringLitLength = RegisterFile[LibUtils.REG_LIST["a2"]];
                            StringBuilder buffer = new();
                            if (FileDescriptor == 1)
                            {
                                while (StringLitLength-- > 0)
                                {
                                    buffer.Append((char)DataMemory.GetByte((int)StringLitAddress));
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
                            int ExitCode = (int)RegisterFile[LibUtils.REG_LIST["a0"]];
                            if (OutputFilePath != null)
                                GenerateRegFileDMStates(OutputFilePath);
                            Environment.Exit(ExitCode);
                        }
                        else
                        {
                            Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported syscall {syscall}\n", 1);
                        }
                        PC += 4;
                        break;
                    }
                case "0000011":
                    {
                        (int rs1, string imm12_, int rd) = GetItypeInst(mc);
                        switch (Funct3)
                        {
                            case "000":
                                {
                                    Shartilities.TODO("sb");
                                    int imm = Convert.ToInt32(imm12_.PadLeft(32, imm12_[0]), 2);
                                    // load data -> convert to binary -> get [7:0] -> SignExtend it -> convert to Int32 -> save in register file
                                    string b = DataMemory.GetByte((int)RegisterFile[rs1] + imm).ToString();
                                    string LoadedDataBin = LibUtils.GetFromIndexLittle(LibUtils.StringToBin(b), 7, 0);
                                    LoadedDataBin = LoadedDataBin.PadLeft(32, LoadedDataBin[0]);
                                    RegisterFile[rd] = Convert.ToInt32(LoadedDataBin, 2);
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
                            case "011": // ld
                                {
                                    Shartilities.TODO("ld");
                                    //int imm = Convert.ToInt32(imm12.PadLeft(32, imm12[0]), 2);
                                    //string LoadedDataBin = LibUtils.GetFromIndexLittle(LibUtils.GetImmediateToBin(DataMemory[(int)RegisterFile[rs1] + imm]), 7, 0);
                                    //LoadedDataBin = LoadedDataBin.PadLeft(32, LoadedDataBin[0]);
                                    //RegisterFile[rd] = Convert.ToInt32(LoadedDataBin, 2);
                                    break;
                                }
                            case "100": // lbu
                                {
                                    Shartilities.TODO("lbu");
                                    int imm = Convert.ToInt32(imm12_.PadLeft(32, imm12_[0]), 2);
                                    string b = DataMemory.GetByte((int)RegisterFile[rs1] + imm).ToString();
                                    string LoadedDataBin = LibUtils.GetFromIndexLittle(LibUtils.StringToBin(b), 7, 0);
                                    LoadedDataBin = LoadedDataBin.PadLeft(32, '0');
                                    RegisterFile[rd] = Convert.ToInt32(LoadedDataBin, 2);
                                    break;
                                }
                            case "101":
                                {
                                    Shartilities.TODO("lhu");
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
                case "1110111":
                    {
                        (int rs1, string imm12, int rd) = GetItypeInst(mc);
                        Int64 t = PC + 4;
                        Int64 imm = Convert.ToInt64(imm12.PadLeft(64, imm12[0]), 2);
                        PC = (int)(RegisterFile[rs1] + (imm & ~1));
                        RegisterFile[rd] = t;
                        break;
                    }
                // end of I-TYPE instructions
                // start of S-TYPE instructions
                case "0100011":
                    {
                        (int rs1, int rs2, string imm12) = GetStypeInst(mc);
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
                            case "011":
                                {
                                    Shartilities.TODO("sd");
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
                        (int rs1, int rs2, string imm12_) = GetStypeInst(mc);
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
                                    Shartilities.Log(Shartilities.LogType.ERROR, $"unsupported Funct3 {Funct3}\n", 1);
                                    break;
                                }
                        }
                        break;
                    }
                // end of S-TYPE instructions
                // start of U-TYPE instructions
                case "0110111":
                    {
                        (int rd, string imm20) = GetUtypeInst(mc);
                        Int64 imm = Convert.ToInt64(imm20.PadLeft(64, '0'), 2) << 12;
                        RegisterFile[rd] = imm;
                        PC += 4;
                        break;
                    }
                case "0010111":
                    {
                        (int rd, string imm20) = GetUtypeInst(mc);
                        Int64 imm = Convert.ToInt64(imm20.PadLeft(64, '0'), 2) << 12;
                        RegisterFile[rd] = PC + imm;
                        PC += 4;
                        break;
                    }
                case "1111111":
                    {
                        (int rd, string imm20) = GetUtypeInst(mc);
                        Int64 imm = Convert.ToInt64(imm20.PadLeft(64, imm20[0]), 2) << 1;
                        RegisterFile[rd] = PC + 4;
                        PC += imm;
                        break;
                    }
                default:
                    {
                        Shartilities.Log(Shartilities.LogType.ERROR, $"unsuppored opcode {opcode}\n", 1);
                        Shartilities.UNREACHABLE("invalid opcode");
                        break;
                    }
                // end of U-TYPE instructions
            }
        }
        public static void Run(List<string> MachingCodes, List<string> DataMemoryInit, uint IM_SIZE, uint DM_SIZE, string? inputOutputFilePath = null)
        {
            PC = 0;
            CyclesConsumed = 0;
            OutputFilePath = inputOutputFilePath;
            InstructionMemory = [];
            RegisterFile = new();
            DataMemory = new(DataMemoryInit, DM_SIZE);

            foreach (string code in MachingCodes)
            {
                InstructionMemory.Add(LibUtils.GetFromIndexLittle(code, 1 * 8 - 1, 0 * 8));
                InstructionMemory.Add(LibUtils.GetFromIndexLittle(code, 2 * 8 - 1, 1 * 8));
                InstructionMemory.Add(LibUtils.GetFromIndexLittle(code, 3 * 8 - 1, 2 * 8));
                InstructionMemory.Add(LibUtils.GetFromIndexLittle(code, 4 * 8 - 1, 3 * 8));
            }
            int imcount = InstructionMemory.Count;
            for (int i = 0; i < IM_SIZE - imcount; i++)
                InstructionMemory.Add("00000000");



            while (CyclesConsumed < 50)
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
                sb.Append(LibUtils.GetRegs(RegisterFile.Registers));
                sb.Append(LibUtils.GetDM(DataMemory.memory));
                sb.Append($"Number of cycles consumed : {CyclesConsumed,10}\n");
                File.WriteAllText(OutPutFilePath, sb.ToString());
                Shartilities.Log(Shartilities.LogType.INFO, $"Generated CAS output of singlecyle in path {OutPutFilePath} successfully\n");
            }
        }
    }
}
