using Assembler;
using System.Text;

namespace LibUtils
{
    public static class LibUtils
    {
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
                        if (!uint.TryParse(Assembler.Assembler.GetImmediate(DataMemoryValues[i]), out uint count))
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
        public static void print_regs(List<int> regs)
        {
            Console.Write("Register file content : \n");
            int i = 0;
            foreach (int n in regs)
            {
                string temp = $"index = {i++,10} , reg_out : signed = {n,10} , unsigned = {(uint)n,10}\n";
                Console.Write(temp);
            }
        }
        public static void print_DM(List<string> DM)
        {
            Console.Write("Data Memory Content : \n");
            int i = 0;
            foreach (string mem in DM)
            {
                string temp;
                temp = $"Mem[{i++,4}] = {mem,11}\n";

                Console.Write(temp);
            }
        }
        public static StringBuilder get_regs(List<int> regs)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Register file content : \n");
            int i = 0;
            foreach (int n in regs)
            {
                string temp = $"index = {i++,10} , reg_out : signed = {n,11} , unsigned = {(uint)n,10}\n";
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
    }
}
