using System.Reflection;
using Spectre.Console;

// NOTE: must be class, struct compiles but dont work, needs public get and setters, nullable is ok, is set to null on empty entry
class Digimon{
    public int number{get; set;}
    public string digimon{get; set;} = "";
    public string stage{get; set;} = "";
    public string type{get; set;} = "";
    public string attribute{get; set;} = "";
    public int memory{get; set;}
    public int equip_slots{get; set;}
    public int lv_50_hp{get; set;}
    public int lv_50_sp{get; set;}
    public int lv_50_atk{get; set;}
    public int lv_50_def{get; set;}
    public int lv_50_int{get; set;}
    public int lv_50_spd{get; set;}
}
class Support{
    public string name{get; set;} = "";
    public string description{get; set;} = "";
}
class Move{
    public string move{get; set;} = "";
    public int sp_cost{get; set;}
    public string type{get; set;} = "";
    public int power{get; set;}
    public string attribute{get; set;} = "";
    public bool inheritable{get; set;}
    public string description{get; set;} = "";
}

class TypeCheck{
    public enum STATUS{
        TYPE,
        NULLABLE_TYPE,
        NOT_TYPE
    }
    public static bool is_nullable(Type t){
        return Nullable.GetUnderlyingType(t) != null;
    }
    public static STATUS is_type<T>(Type t){
        if(t == typeof(T)) return STATUS.TYPE;
        if(Nullable.GetUnderlyingType(t) == typeof(T)) return STATUS.NULLABLE_TYPE;
        return STATUS.NOT_TYPE;
    }
}

class CSV<T> where T : new(){
    public static (bool, int) parse_to_int(string str)
    {
        int n = -1;
        bool return_value = int.TryParse(str, out n);
        return (return_value, n);
    }
    public static (bool, double) parse_to_double(string str)
    {
        double n = -1;
        bool return_value = double.TryParse(str, out n);
        return (return_value, n);
    }
    public static (bool, bool) parse_to_bool(string str)
    {
        str = str.ToLower().Trim();
        if(str == "yes") return (true, true);
        if(str == "no") return (true, false);
        return (false, false);
    }
    private static string[] sep(string line){
        List<string> buffer = new();
        string str = "";
        bool is_str = false;
        foreach(char ch in line){
            if(ch == '"'){
                is_str = !is_str;
                continue;
            }
            if(ch == ',' && !is_str){
                buffer.Add(str);
                str = "";
                continue;
            }
            str+=ch;
        }
        buffer.Add(str);
        return buffer.ToArray();
    }
    public static (string, List<T>) csv_parser(Func<string[], string[], T> add_function, string file){
        StreamReader reader = new StreamReader(file);
        List<T> result = new();
        string? format = reader.ReadLine();
        if(format == null){
            throw new Exception("");
        }
        for(var str = reader.ReadLine(); str != null; str = reader.ReadLine()){
            result.Add(add_function(sep(format), sep(str)));
        }
        reader.Close();
        return (format, result);
    }
    private enum CHARACTER_TYPE : int{
        LETTER,
        NUMBER,
        WHITESPACE,
        UNDERSCORE,
        UNSET
    }
    private static CHARACTER_TYPE get_type_for_ch(char ch){
        if(ch == ' ') return CHARACTER_TYPE.WHITESPACE;
        if(ch == '_') return CHARACTER_TYPE.UNDERSCORE;
        if(char.IsNumber(ch)) return CHARACTER_TYPE.NUMBER;
        if(char.IsLetter(ch)) return CHARACTER_TYPE.LETTER;
        throw new Exception("invalid typename");
    }

    private static string format_string(string str){
        string result = "";
        CHARACTER_TYPE prev = CHARACTER_TYPE.UNSET;
        Func<bool> should_insert_underscore = () => {
            if(result == "") return false;
            if(result[result.Length - 1] == '_') return false;
            return true;
        };
        Func<bool> try_publish_underscore = () =>{
            if(should_insert_underscore()){
                    result += '_';
                    prev = CHARACTER_TYPE.UNDERSCORE;
                    return true;
            }
            return false;
        };
        foreach(var ch in str){
            CHARACTER_TYPE t = get_type_for_ch(ch);

            if(t == CHARACTER_TYPE.WHITESPACE){
                try_publish_underscore();
                continue;
            }
            if(t == CHARACTER_TYPE.UNDERSCORE){
                try_publish_underscore();
                continue;
            }
            if(prev == CHARACTER_TYPE.UNSET){
                result += ch;
                prev = t;
            }
            else if(t == prev){
                result += ch;
                prev = t;
            }
            else{
                try_publish_underscore();
                result += ch;
                prev = t;
            }
        }
        return result.ToLower();
    }
    public static T map_csv_entry_to_type(string[] format, string[] data){
        if(format.Length != data.Length) throw new Exception("");
        T output = new T();
        for(int i = 0; i < format.Length; ++i){
            var format_str = format_string(format[i]);
            PropertyInfo? property = output.GetType().GetProperty(format_str);
            if(property != null){
                if(data[i] == ""){
                    bool nullable = TypeCheck.is_nullable(property.PropertyType);
                    if(!nullable) throw new Exception("trying to assign empty object to non-nullable type");
                    property.SetValue(output, null);
                }
                else if(TypeCheck.is_type<int>(property.PropertyType) != TypeCheck.STATUS.NOT_TYPE){
                    var(ok, num) = parse_to_int(data[i]);
                    if(!ok) throw new Exception("not integer");
                    property.SetValue(output, num);
                }
                else if(TypeCheck.is_type<double>(property.PropertyType) != TypeCheck.STATUS.NOT_TYPE){
                    var(ok, num) = parse_to_double(data[i]);
                    if(!ok) throw new Exception("not double");
                    property.SetValue(output, num);
                }
                else if(TypeCheck.is_type<string>(property.PropertyType) != TypeCheck.STATUS.NOT_TYPE){
                    property.SetValue(output, data[i]);
                }
                else if(TypeCheck.is_type<bool>(property.PropertyType) != TypeCheck.STATUS.NOT_TYPE){
                    var(ok, boolean) = parse_to_bool(data[i]);
                    if(!ok) throw new Exception("invalid boolean");
                    property.SetValue(output, boolean);
                }
                else{
                    throw new Exception("unsupported data_type");
                }
            }
            else{
                throw new Exception("no corresponding entry");
            }
        }
        return output;
    }
}

class App{
    static void Main(string[] args){
        string digimonlist_csv = ".\\Digimon\\DigiDB_digimonlist.csv";
        string movelist_csv = ".\\Digimon\\DigiDB_movelist.csv";
        string supportlist_csv = ".\\Digimon\\DigiDB_supportlist.csv";

        var (format_digimon, digimon_data) = CSV<Digimon>.csv_parser((string[] format, string[] data) => {
                return CSV<Digimon>.map_csv_entry_to_type(format, data);
            }, digimonlist_csv);
        var (format_move, movelist_data) = CSV<Move>.csv_parser((string[] format, string[] data) => {
                return CSV<Move>.map_csv_entry_to_type(format, data);
            }, movelist_csv);
        var (format_support, supportlist_data) = CSV<Support>.csv_parser((string[] format, string[] data) => {
                return CSV<Support>.map_csv_entry_to_type(format, data);
            }, supportlist_csv);

        SelectionPrompt<string> selection = new();
        foreach(var digimon in digimon_data){
            selection.AddChoice(digimon.digimon);
        }
        AnsiConsole.Clear();

        AnsiConsole.WriteLine("Select a digimon to return a list of available moves");
        var result = AnsiConsole.Prompt(selection);
        if(result != null){
            IEnumerable<Digimon> query = from digi in digimon_data where digi.digimon == result select (Digimon)digi;
            var list = query.ToList();
            if(list.Count == 1){
                IEnumerable<Move> moves = from m in movelist_data where list[0].attribute == m.attribute select (Move)m;

                List<Markup> rows = new();
                foreach(var m in moves){
                    rows.Add(new Markup(m.move + ", " +  m.sp_cost + ", " + m.type + ", " + m.power + ", " + m.attribute + ", " + m.inheritable + ", " + m.description));
                }
                AnsiConsole.Clear();
                AnsiConsole.WriteLine(format_move);
                AnsiConsole.Write(new Rows(rows));
            }
            else{
                throw new Exception("multiple of the same entry");
            }
        }
    }
}
