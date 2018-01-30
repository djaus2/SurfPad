using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

using Windows.Data.Json;

/// <summary>
/// The Commands class and the MainPage functions relating to it
/// </summary>
namespace roundedbox
{
    public class Point
    {
        public int Col;
        public int Row;
        public Point(int x, int y)
        {
            Col = x;
            Row = y;
        }

        public string ToPt()
        {
            return "(" + Col.ToString() + "," + Row.ToString() + ")";
        }
    }
    public class Commands
    {
        /// <summary>
        /// Create a new command with name and url only
        /// </summary>
        /// <param name="Name">As displayed on the command button</param>
        /// <param name="Url">Relative URL passed to web portal</param>
        //public Commands(string Menu, string Name, Point Id)
        //{
        //    menu = Menu;
        //    name = Name;
        //    idTag = Id;
        //    CommandsList.Add(this);
        //}

        public const string cSensorsRowIndexKey = "iSensorsRowIndex";
        public const string cCommandActionsColIndexKey = "iCommandActionsColIndex";
        public const string cValuesRowIndexKey = "iValuesRowIndex";
        public const string cComPortIdKey = "sComPortId";
        public const string cComportConnectDeviceNoKey = "iComportConnectDeviceNo";
        public const string cFTDIComPortIdKey = "sFDTIComPortId";
        public const string cFTDIComportConnectDeviceNoKey = "iFTDIComportConnectDeviceNo";
        public const string cCommentPrefix = "Comment_";

        public static bool CheckKeys()
        {
            if (!ElementConfigInt.ContainsKey(cSensorsRowIndexKey))
            {
                System.Diagnostics.Debug.WriteLine("ElementConfigInt key iSensorsRowIndex Key not found.");
                return false;
            }
            if (!ElementConfigInt.ContainsKey(cCommandActionsColIndexKey))
            {
                System.Diagnostics.Debug.WriteLine("ElementConfigInt iCommandActionsColIndex Key not found.");
                return false;
            }
            if (!ElementConfigInt.ContainsKey(cValuesRowIndexKey))
            {
                System.Diagnostics.Debug.WriteLine("ElementConfigInt iValuesRowIndex Key  not found.");
                return false;
            }
            return true;
        }
        public static bool CheckComportIdSettingExists()
        {
            if (!ElementConfigStr.ContainsKey(cComPortIdKey))
            {
                System.Diagnostics.Debug.WriteLine("Optional ElementConfigCh sComPortId Key  not found.");
                return false;
            }
            else
            if (ElementConfigStr[cComPortIdKey] == "")
            {
                System.Diagnostics.Debug.WriteLine("Optional ElementConfigCh iComPortIdKey  is \"\".");
                return false;
            }
            return true;
        }

        public static bool CheckcIfComportConnectDeviceNoKeySettingExists()
        {
            if (!ElementConfigInt.ContainsKey(cComportConnectDeviceNoKey))
            {
                System.Diagnostics.Debug.WriteLine("Optional ElementConfigInt key iComportConnectDeviceNoKey  not found.");
                return false;
            }
            if (ElementConfigInt[cComportConnectDeviceNoKey] < 0)
            {
                System.Diagnostics.Debug.WriteLine("Optional ElementConfigInt key iComportConnectDeviceNoKey  invalid value <0.");
                return false;
            }

            return true;
        }
        public Commands(string Menu, string Name, int Col, int Row)
        {

            menu = Menu;
            name = Name;
            idTag = new Point(Col, Row);
            if (Col > MaxIdTag.Col)
                MaxIdTag = new Point(Col, MaxIdTag.Row);
            if (Row > MaxIdTag.Row)
                MaxIdTag = new Point(MaxIdTag.Col, Row);
            CommandsList.Add(this);

            if ((Row == Commands.ElementConfigInt[cSensorsRowIndexKey]) && (Col != Commands.ElementConfigInt[cCommandActionsColIndexKey]))
            {
                Array.Resize(ref Sensors, Sensors.Length + 1);
                Sensors[Sensors.Length - 1] = Name;
            }
            else if (Col == Commands.ElementConfigInt[cCommandActionsColIndexKey])
            {
                Array.Resize(ref CommandActions, CommandActions.Length + 1);
                CommandActions[CommandActions.Length - 1] = Name.Substring(1);
            }
        }
        public static Dictionary<string, int> ElementConfigInt { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, char> ElementConfigCh { get; set; } = new Dictionary<string, char>();
        public static Dictionary<string, string> ElementConfigStr { get; set; } = new Dictionary<string, string>();

        public static void Init()
        {
            Commands.Sensors = new string[0];
            Commands.CommandActions = new string[0];
            Commands.CommandsList = new List<Commands>();
            Commands.ElementConfigInt = new Dictionary<string, int>();
            Commands.ElementConfigCh = new Dictionary<string, char>();
            Commands.ElementConfigStr = new Dictionary<string, string>();
        }



        /// <summary>
        /// The name of the menu. 
        /// Their can be more than one in the Json file
        /// </summary>
        public string menu { get; set; } = "";

        /// <summary>
        /// As displayed on the command button
        /// This is used to determine the action in the Button_Click event handler
        /// </summary>
        public string name { get; set; } = "";

        /// <summary>
        /// The (column,row) of the button
        /// </summary>
        public Point idTag { get; set; } = new Point(0, 0);

        public static Point MaxIdTag { get; set; } = new Point(0, 0);

        //If false then button will be effectively just a TextBlock
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets a specific button's text using its menu name and id
        /// </summary>
        /// <param name="Menu">Menu name</param>
        /// <param name="x">Button column</param>
        /// <param name="y">Button row</param>
        /// <returns></returns>
        public static Commands GetCommand(string Menu, int x, int y)
        {
            Commands cmd = null;
            try
            {
                var menuList = from m in CommandsList where (m.menu == Menu) && (m.idTag.Col == x) && (m.idTag.Row == y) select m;
                if (menuList != null)
                    if (menuList.Count() != 0)
                        cmd = menuList.First();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
            return cmd;
        }

        public static List<Commands> CommandsList { get; set; } = new List<Commands>();

        public static List<Commands> GetMenu(string Menu)
        {
            var menuList = from m in CommandsList where m.menu == Menu select m;
            return menuList.ToList<Commands>();
        }

        public static string[] Sensors = new string[0];
        public static string[] CommandActions = new string[0];
    }



    public sealed partial class MainPage : Page
    {

        /// <summary>
        /// Called at startup to load the commands into a list
        /// The structure of the Json file is:
        /// An array of named menus
        ///     Each Menu is an array of an array of strings
        ///         Each string represents a command button (the content to display..
        ///     So each menu represents an array of an array of command buttons
        ///         Or each menu is a set of rows or command buttons
        ///     Note each row can contain one or more commands.
        ///         Don't have to have same number on each row.
        /// </summary>
        /// <param name="Menu">Either All Commands ("") or One menu name</param>
        private void GetCommands(string Menu)
        {

            JsonArray ResultData = null;
            using (StreamReader file = File.OpenText(".\\Data\\menus.json"))
            {
                String JSONData;

                //Get the stream as text.
                JSONData = file.ReadToEnd();

                //Json data is an array of arrays
                //These are then either an array of strings or just a string.
                //Convert to JSON object
                ResultData = (JsonArray)JsonArray.Parse(JSONData);
            }

            if (ResultData != null)
            {
                JsonArray jMenu = null;

                //For each menu (named object in the array)
                foreach (var varJo in ResultData)
                {
                    JsonObject jo = (JsonObject)varJo.GetObject();
                    string menu = jo.Keys.First();
                    if (Menu != menu)
                        continue;
                    //{
                    //    if (!jo.ContainsKey(Menu))
                    //        return;
                    //}
                    //else if (menu == "ElementConfig")
                    //    continue;                 

                    jMenu = jo.GetNamedArray(menu);
                    if (jMenu != null)
                    {
                        //For each row in that menu
                        for (int row = 0; row < jMenu.Count; row++)
                        {
                            //For each command in that menu
                            JsonValue jv = (JsonValue)jMenu[row];
                            if (jv.ValueType.ToString().ToLower() == "array")
                            {
                                JsonArray ja = jv.GetArray();
                                for (int col = 0; col < ja.Count; col++)
                                {
                                    JsonValue jv2 = (JsonValue)ja[col];
                                    //Got command so add to list (done automatically when instantiated).
                                    if (Menu == "ElementConfig")
                                    {
                                        JsonObject jo2 = (JsonObject)jv2.GetObject();
                                        string config = jo2.Keys.First();
                                        //Ignore comments in ElementConfig
                                        if (config.Length >= Commands.cCommentPrefix.Length)
                                            if (config.Substring(0, Commands.cCommentPrefix.Length) == Commands.cCommentPrefix)
                                                continue;

                                        //if (jo2.Values.First().ValueType.ToString() == "Number")
                                        //    Commands.ElementConfigInt.Add(config, (int)jo2.Values.First().GetNumber());
                                        //else if (jo2.Values.First().ValueType.ToString() == "String")
                                        //    //Most of these configs are chars. cComPortIdKey is an except.Tod0: Improve this.
                                        //    if (config != Commands.cComPortIdKey)
                                        //    Commands.ElementConfigCh.Add(config, ((string)jo2.Values.First().GetString())[0]);
                                        //else
                                        //    Commands.ElementConfigStr.Add(config, (string)jo2.Values.First().GetString());
                                        switch (config[0]) //First char is i, c or s = int, char or string
                                        {
                                            case 'i':
                                                Commands.ElementConfigInt.Add(config, (int)jo2.Values.First().GetNumber());
                                                break;
                                            case 'c':
                                                Commands.ElementConfigCh.Add(config, ((string)jo2.Values.First().GetString())[0]);
                                                break;
                                            case 's':
                                                Commands.ElementConfigStr.Add(config, (string)jo2.Values.First().GetString());
                                                break;
                                        }
                                    }
                                    else
                                    {
                                        string name = jv2.GetString();
                                        Commands cmd = new Commands(menu, name, col, row);
                                    }
                                }
                            }
                            else if (jv.ValueType.ToString().ToLower() == "string")
                            {
                                //Handles case where row is just one string rather than an array
                                // ie can Handle one entry on a row not in an array
                                Commands cmd = new Commands(menu, jv.GetString(), row, 0);
                            }

                        }
                    }
                    //If not getting all menus then must be done so break.
                    //if (Menu != "")
                    break;
                }

            }
        }

    }

}



