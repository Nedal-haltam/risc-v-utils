using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LibUtils
{
    public static class LibUtils
    {
        public static void assert(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(msg);
            Console.ResetColor();
            Environment.Exit(1);
        }
        public static void clean_comments(ref List<string> code)
        {
            for (int i = 0; i < code.Count; i++)
            {
                if (code[i].Contains("//"))
                {
                    code[i] = code[i][..code[i].IndexOf('/')];
                }
                else if (code[i].Contains('#'))
                {
                    code[i] = code[i][..code[i].IndexOf('#')];
                }

            }
        }
        public static (List<string>, List<string>, List<KeyValuePair<string, int>>) assemble_data_dir(List<string> data_dir)
        {
            List<string> data = [];
            List<KeyValuePair<string, int>> addresses = [];
            int address = 0;
            for (int i = 0; i < data_dir.Count; i++)
            {
                if (data_dir[i].IndexOf(":") == -1)
                    continue;
                string name = data_dir[i].Substring(0, data_dir[i].IndexOf(":"));
                if (data_dir[i].Substring(data_dir[i].IndexOf(":")+1).IndexOf(":") == -1)
                {
                    name = name.Trim();
                    int addr = address;
                    address += data_dir[i].Count(s => s == ',') + 1;
                    addresses.Add(new KeyValuePair<string, int>(name, addr));
                }
                else
                {
                    name = name.Trim();
                    string temp = data_dir[i];
                    string[] datas = data_dir[i].Substring(temp.IndexOf(":")).Replace(".word", "").Split(':');
                    int addr = address;
                    address += Convert.ToInt32(datas[2]) - Convert.ToInt32(datas[1]) + 1;
                    addresses.Add(new KeyValuePair<string, int>(name, addr));
                }
            }
            for (int i = 0; i < data_dir.Count; i++)
            {
                int index = data_dir[i].IndexOf(':');
                if (index != -1)
                {
                    string line = data_dir[i].Substring(index + 1);
                    line = line.Trim();
                    line = line.Replace(".word", "");
                    if (line.Contains(':'))
                    {
                        List<string> vals = line.Split(':').ToList();
                        int count = Convert.ToInt32(vals[1]) - Convert.ToInt32(vals[0]) + 1;
                        for (int j = 0; j < count; j++)
                        {
                            data.Add("0");
                        }
                    }
                    else
                    {
                        List<string> vals = line.Split(',').ToList();
                        foreach (string val in vals)
                        {
                            int number = 0;
                            string snum = val.ToLower().Trim();
                            try
                            {
                                if (snum.StartsWith("0x"))
                                    number = Convert.ToInt32(snum, 16);
                                else
                                    number = Convert.ToInt32(snum);
                            }
                            catch (Exception)
                            {
                                number = 0;
                                //throw new Exception("invalid number");
                            }
                            data.Add(number.ToString());
                        }
                    }
                }
            }
            List<string> DM_INIT = [];
            List<string> DM_vals = [];
            for (int i = 0; i < data.Count; i++)
            {
                DM_vals.Add(data[i].ToString());
                if (data[i][0] == '-')
                {
                    string temp = $"DataMem[{i,2}] <= -32'd{data[i].Substring(1)};";
                    DM_INIT.Add(temp);
                }
                else
                {
                    string temp = $"DataMem[{i,2}] <= 32'd{data[i]};";
                    DM_INIT.Add(temp);
                }
            }

            return (DM_INIT, DM_vals, addresses);
        }
        public static (List<string> , List<string>) Get_directives(List<string> src)
        {
            for (int i = 0; i < src.Count; i++)
            {
                src[i] = src[i].Trim();
            }

            int data_index = src.IndexOf(".data");
            int text_index = src.IndexOf(".text");

            List<string> curr_data_dir = [];
            List<string> curr_text_dir = [];

            if (data_index != -1 && text_index != -1)
            {
                curr_data_dir = src.GetRange(data_index, text_index - data_index);
            }
            if (text_index != -1)
            {
                curr_text_dir = src.GetRange(text_index + 1, src.Count - text_index - 1);
            }
            return (curr_data_dir, curr_text_dir);
        }
        public static string GetMIFentry(string addr, string value)
        {
            return $"{addr} : {value};";
        }
        public static StringBuilder GetMIFHeader(int width, int depth, string address_radix, string data_radix)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"WIDTH={width};\n");
            sb.Append($"DEPTH={depth};\n");
            sb.Append($"ADDRESS_RADIX={address_radix};\n");
            sb.Append($"DATA_RADIX={data_radix};\n");
            sb.Append("CONTENT BEGIN\n");

            return sb;
        }
        public static StringBuilder ToMIFentries(int start_address, List<string> list, int width, int from_base)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < list.Count; i++)
            {
                string address = (start_address + i).ToString("X");
                string value = Convert.ToInt32(list[i], from_base).ToString("X").PadLeft(width / 4, '0');
                string entry = GetMIFentry(address, value);
                sb.Append(entry + '\n');
            }

            return sb;
        }
        public static StringBuilder GetMIFTail()
        {
            return new StringBuilder("END;\n");
        }
        public static StringBuilder GetDMMIF(List<string> DM, int width, int depth, int from_base)
        {
            StringBuilder sb = new();
            sb.Append(GetMIFHeader(width, depth, "HEX", "HEX"));
            sb.Append(ToMIFentries(0, DM, width, from_base));
            sb.Append($"[{DM.Count:X}..{(depth - 1):X}] : 0;\n");
            sb.Append(GetMIFTail());

            return sb;
        }
        public static StringBuilder GetIMMIF(List<string> mc, int width, int depth, int from_base)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(GetMIFHeader(width, depth, "HEX", "HEX"));
            sb.Append(ToMIFentries(0, mc, width, from_base));
            sb.Append(GetMIFTail());

            return sb;
        }
        public static string get_entry_IM_INIT(string mc, string inst, int i)
        {
            string hex = Convert.ToInt32(mc, 2).ToString("X").PadLeft(8, '0');
            return ($"InstMem[{i,2}] <= 32'h{hex};// {inst,-20}").Trim();
        }
        public static List<string> get_IM_INIT(List<string> mc, List<string> curr_insts)
        {
            List<string> IM_INIT = [];
            for (int i = 0; i < mc.Count; i++)
            {
                IM_INIT.Add(get_entry_IM_INIT(mc[i], curr_insts[i], i));
            }
            return IM_INIT;
        }

        public static Dictionary<int, bool> ReadMLPrediction(string filepath)
        {
            string[] list = File.ReadAllLines(filepath);
            Dictionary<int, bool> LUT = [];
            for (int i = 1; i < list.Length; i++)
            {
                string[] entry = list[i].Split(',');
                if (entry.Length != 4)
                    throw new Exception($"entry number {i} is not like other entries");
                LUT[Convert.ToInt32(entry[0], 16)] = entry[3] == "1";
            }
            return LUT;
        }
    }
}
