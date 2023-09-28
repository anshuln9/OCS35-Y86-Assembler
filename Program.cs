using System.Text.RegularExpressions;

namespace ocs35_y86_assembler;
// Uses Big Endian instead of Little Endian
public class Program
{
    static int position = 0;
    static string label = @"^[a-zA-Z]+[a-zA-Z0-9]*:";
    static string directive = @"^\.pos\s.*|^\.align\s.*|^\.long\s.*|^\.quad\s.*";
    static string instruction = @"^(halt|nop|rrmovl|irmovl|rmmovl|mrmovl|addl|subl|andl|xorl|jmp|jle|jl|je|jne|jge|jg|cmovle|cmovl|cmove|cmovne|cmovge|cmovg|call|ret|pushl|popl)\s?.*,?.*";
    static Dictionary<string, string> registers = new Dictionary<string, string>() {
        {"%eax", "0"},
        {"%ecx", "1"},
        {"%edx", "2"},
        {"%ebx", "3"},
        {"%esp", "4"},
        {"%ebp", "5"},
        {"%esi", "6"},
        {"%edi", "7"}
    };
    static Dictionary<string, int> labels = new Dictionary<string, int>();
    static List<string> outputs = new List<string>();
    static void Main(string[] args)
    {


        string[] lines = File.ReadAllLines(@"input.txt");

        foreach (string unsafeLine in lines)
        {
            string line = unsafeLine.Trim().ToLower();
            // Check if line is a label
            if (Regex.IsMatch(line, label))
            {
                int colonPos = line.IndexOf(":");
                string label = line.Substring(0, colonPos);
                labels.Add(label, position);
                line = line.Substring(colonPos + 1).Trim();
            }
            // Check if line is a directive
            if (Regex.IsMatch(line, directive))
            {
                CallDirective(line);
            }
            // Check if line is an instruction
            else if (Regex.IsMatch(line, instruction))
            {
                CallInstruction(line);
            }
        }

        // iterate through all outputs, check if it contains label and replace with correct value, then write to file
        var file = File.Create(@"output.yo");
        using StreamWriter stream = new(file);

        foreach (string output in outputs)
        {
            string newOutput = output;
            var label = Regex.Match(output, @"#[a-zA-Z]+[a-zA-Z0-9]*#");
            if (label.Success)
            {
                newOutput = output.Replace(label.Value, labels[label.Value[1..^1]].ToString("X8"));
            }
            stream.WriteLine(newOutput);
        }
    }

    static void CallDirective(string line)
    {
        string[] split = line.Split(" ");
        string directive = split[0];
        string arg = split[1];
        switch (directive)
        {
            case ".pos":
                Pos(arg);
                break;
            case ".align":
                Align(arg);
                break;
            case ".long":
                Long(arg);
                break;
            case ".quad":
                Quad(arg);
                break;
            default:
                break;
        }
    }

    // Assumes that pos is always in the form .pos 0x1234 or .pos 1234
    static void Pos(string arg)
    {
        int value = parseInt(arg);
        position = value;
    }

    // Assumes that align is always in the form .align 0x1234 or .align 1234
    static void Align(string arg)
    {
        int value = parseInt(arg);
        while (position % value != 0) position++;
    }

    // Assumes that long is always in the form .long 0x1234 or .long 1234
    static void Long(string arg)
    {
        int value = parseInt(arg);
        Output(value.ToString("X8"));
        position += 4;
    }

    // Assumes that quad is always in the form .quad 0x1234 or .quad 1234
    static void Quad(string arg)
    {
        int value = parseInt(arg);
        Output(value.ToString("X16"));
        position += 8;
    }

    static void CallInstruction(string line)
    {
        // Split instruction and arguments
        int spacePos = line.IndexOf(" ");
        if (spacePos == -1) spacePos = line.Length;
        string instruction = line.Substring(0, spacePos).Trim();

        if (instruction == "halt")
        {
            Halt();
            return;
        }
        else if (instruction == "nop")
        {
            Nop();
            return;
        }
        else if (instruction == "ret")
        {
            Ret();
            return;
        }

        string[] args = line.Substring(spacePos + 1).Split(",");
        if (args.Length > 0) args[0] = args[0].Trim();
        if (args.Length > 1) args[1] = args[1].Trim();

        switch (instruction)
        {
            case "rrmovl":
                Rrmovl(args);
                break;
            case "irmovl":
                Irmovl(args);
                break;
            case "rmmovl":
                Rmmovl(args);
                break;
            case "mrmovl":
                Mrmovl(args);
                break;
            case "addl":
                Addl(args);
                break;
            case "subl":
                Subl(args);
                break;
            case "andl":
                Andl(args);
                break;
            case "xorl":
                Xorl(args);
                break;
            case "jmp":
                Jmp(args, 0);
                break;
            case "jle":
                Jmp(args, 1);
                break;
            case "jl":
                Jmp(args, 2);
                break;
            case "je":
                Jmp(args, 3);
                break;
            case "jne":
                Jmp(args, 4);
                break;
            case "jge":
                Jmp(args, 5);
                break;
            case "jg":
                Jmp(args, 6);
                break;
            case "cmovle":
                Cmov(args, 1);
                break;
            case "cmovl":
                Cmov(args, 2);
                break;
            case "cmove":
                Cmov(args, 3);
                break;
            case "cmovne":
                Cmov(args, 4);
                break;
            case "cmovge":
                Cmov(args, 5);
                break;
            case "cmovg":
                Cmov(args, 6);
                break;
            case "call":
                Call(args);
                break;
            case "pushl":
                Pushl(args);
                break;
            case "popl":
                Popl(args);
                break;
            default:
                break;
        }
    }

    static void Halt()
    {
        string output = "00";
        Output(output);
        position += 1;
    }

    static void Nop()
    {
        string output = "10";
        Output(output);
        position += 1;
    }

    static void Rrmovl(string[] args)
    {
        string output = "20";
        string rA = registers[args[0]];
        string rB = registers[args[1]];
        output += rA;
        output += rB;
        Output(output);
        position += 2;
    }

    // Assumes that irmovl is always in the form irmovl $0x12345678, %eax or irmovl Label, %eax
    static void Irmovl(string[] args)
    {
        string output = "30F";
        string V = args[0];
        string rB = registers[args[1]];

        if (V.Length < 1) V = "0";
        if (Regex.IsMatch(V, @"^[a-zA-Z]+[a-zA-Z0-9]*"))
        {
            V = $"#{V}#";
        }
        else
        {
            // converts to hex that is 4 bytes long (padding in front with 0s)
            V = parseInt(V).ToString("X8");
        }

        output += rB;
        output += V;
        Output(output);
        position += 6;
    }

    // Assumes that rmmovl is always in the form rmmovl %eax, 0x12345678(%ebx) or rmmovl %eax, Label(%ebx)
    static void Rmmovl(string[] args)
    {
        string output = "40";
        string rA = registers[args[0]];
        string DrB = args[1];
        string D = DrB[0..^6];
        string rB = registers[DrB[^5..^1]];

        // check if D is a label
        if (D.Length < 1) D = "0";
        if (Regex.IsMatch(D, @"^[a-zA-Z]+[a-zA-Z0-9]*"))
        {
            D = $"#{D}#";
        }
        else
        {
            // converts to hex that is 4 bytes long (padding in front with 0s)
            D = parseInt(D).ToString("X8");
        }

        output += rA;
        output += rB;
        output += D;
        Output(output);
        position += 6;
    }

    // Assumes that mrmovl is always in the form mrmovl 0x12345678(%ebx), %eax or mrmovl Label(%ebx), %eax
    static void Mrmovl(string[] args)
    {
        string output = "50";
        string rA = registers[args[1]];
        string DrB = args[0];
        string D = DrB[0..^6];
        string rB = registers[DrB[^5..^1]];

        // check if D is a label
        if (D.Length < 1) D = "0";
        if (D.Length < 1) D = "0";
        if (Regex.IsMatch(D, @"^[a-zA-Z]+[a-zA-Z0-9]*"))
        {
            D = $"#{D}#";
        }
        else
        {
            // converts to hex that is 4 bytes long (padding in front with 0s)
            D = parseInt(D).ToString("X8");
        }

        output += rA;
        output += rB;
        output += D;
        Output(output);
        position += 6;
    }

    static void Addl(string[] args)
    {
        string output = "60";
        string rA = registers[args[0]];
        string rB = registers[args[1]];
        output += rA;
        output += rB;
        Output(output);
        position += 2;
    }

    static void Subl(string[] args)
    {
        string output = "61";
        string rA = registers[args[0]];
        string rB = registers[args[1]];
        output += rA;
        output += rB;
        Output(output);
        position += 2;
    }

    static void Andl(string[] args)
    {
        string output = "62";
        string rA = registers[args[0]];
        string rB = registers[args[1]];
        output += rA;
        output += rB;
        Output(output);
        position += 2;
    }

    static void Xorl(string[] args)
    {
        string output = "63";
        string rA = registers[args[0]];
        string rB = registers[args[1]];
        output += rA;
        output += rB;
        Output(output);
        position += 2;
    }

    // Assumes that jXX is always in the form jXX 0x12345678 or jXX Label
    static void Jmp(string[] args, int fn)
    {
        string output = $"7{fn}";
        string D = args[0];

        if (D.Length < 1) D = "0";
        if (D.Length < 1) D = "0";
        if (Regex.IsMatch(D, @"^[a-zA-Z]+[a-zA-Z0-9]*"))
        {
            D = $"#{D}#";
        }
        else
        {
            // converts to hex that is 4 bytes long (padding in front with 0s)
            D = parseInt(D).ToString("X8");
        }
        output += D;
        Output(output);
        position += 5;
    }

    // Assumes that cmovXX is always in the form cmovXX %eax, %ebx
    static void Cmov(string[] args, int fn)
    {
        string output = $"2{fn}";
        string rA = registers[args[0]];
        string rB = registers[args[1]];
        output += rA;
        output += rB;
        Output(output);
        position += 2;
    }

    // Assumes that call is always in the form call 0x12345678 or call Label
    static void Call(string[] args)
    {
        string output = "80";
        string D = args[0];

        if (D.Length < 1) D = "0";
        if (D.Length < 1) D = "0";
        if (Regex.IsMatch(D, @"^[a-zA-Z]+[a-zA-Z0-9]*"))
        {
            D = $"#{D}#";
        }
        else
        {
            // converts to hex that is 4 bytes long (padding in front with 0s)
            D = parseInt(D).ToString("X8");
        }

        output += D;
        Output(output);
        position += 5;
    }

    static void Ret()
    {
        string output = "90";
        Output(output);
        position += 1;
    }

    static void Pushl(string[] args)
    {
        string output = "A0";
        string rA = registers[args[0]];
        output += rA;
        output += "F";
        Output(output);
        position += 2;
    }

    static void Popl(string[] args)
    {
        string output = "B0";
        string rA = registers[args[0]];
        output += rA;
        output += "F";
        Output(output);
        position += 2;
    }

    // converts $4, 4, $0x4, and 0x4 to 4
    static int parseInt(string s)
    {
        // remove $ if it exists
        if (s.StartsWith("$")) s = s.Substring(1);
        // check if empty
        if (s == "") return 0;
        // check if hex or decimal
        if (s.StartsWith("0x")) return Convert.ToInt32(s.Substring(2), 16);
        else return int.Parse(s);
    }

    static void Output(string output)
    {
        outputs.Add($"0x{position.ToString("X")}:    {output}");
    }
}
