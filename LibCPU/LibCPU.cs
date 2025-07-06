using System.Text;
namespace LibCPU
{
    public enum CPU_type
    {
        SingleCycle,
    }
    public static class SingleCycle
    {
        public static (int, List<int>, List<string>) Run(List<string> MachingCodes, List<string> DataMemoryInit, uint IM_SIZE, uint DM_SIZE)
        {
            const int MAX_CLOCKS = 100 * 1000 * 1000;

            string nop = "".PadLeft(32, '0');
            int PC = 0;
            int CyclesConsumed = 0;

            List<string> InstructionMemory = [];
            InstructionMemory.AddRange(MachingCodes);
            int imcount = InstructionMemory.Count;
            for (int i = 0; i < IM_SIZE - imcount; i++) InstructionMemory.Add(nop);

            List<int> RegisterFile = [];
            for (int i = 0; i < 32; i++) RegisterFile.Add(0);

            List<string> DataMemory = [];
            DataMemory.AddRange(DataMemoryInit);
            int dmcount = DataMemory.Count;
            for (int i = 0; i < DM_SIZE - dmcount; i++) DataMemory.Add("0");

            while (CyclesConsumed < MAX_CLOCKS)
            {
                string mc = MachingCodes[PC];

                Shartilities.TODO("here you consume an instruction");

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
