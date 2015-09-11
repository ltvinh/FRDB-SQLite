using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using FRDB_SQLite;

namespace FRDB_SQLite.Class
{
    public class QueryPL
    {
        #region 1. Fields
        private String[] _compare = { ">", "=", "<", "!" };
        #endregion
        #region 2. Properties

        #endregion
        #region 3. Contructors

        #endregion
        #region 4. Methods
        public static void txtQuery_TextChanged(SyntaxRichTextBox txtQuery)
        {
            //textBox1.SelectionStart = 0;
            //textBox1.SelectionLength = textBox1.Text.Length;
            // Add the keywords to the list.
            txtQuery.Settings.Keywords.Add("select");
            txtQuery.Settings.Keywords.Add("from");
            txtQuery.Settings.Keywords.Add("where");
            txtQuery.Settings.Keywords.Add("natural join");
            txtQuery.Settings.Keywords.Add("except");
            txtQuery.Settings.Keywords.Add("union all");
            txtQuery.Settings.Keywords.Add("union");
            txtQuery.Settings.Keywords.Add("intersect");
            txtQuery.Settings.Keywords.Add("descartes");
            txtQuery.Settings.Keywords.Add("left join");
            txtQuery.Settings.Keywords.Add("right join");

            // The operators and logicality
            txtQuery.Settings.Keywords.Add("max");
            txtQuery.Settings.Keywords.Add("top");
            txtQuery.Settings.Keywords.Add("min");
            txtQuery.Settings.Keywords.Add("sum");
            txtQuery.Settings.Keywords.Add("avg");
            txtQuery.Settings.Keywords.Add("count");
            txtQuery.Settings.Keywords.Add("oder by");
            txtQuery.Settings.Keywords.Add("desc");
            txtQuery.Settings.Keywords.Add("asc");

            //txtQuery.Settings.Keywords2.Add("*");
            txtQuery.Settings.Keywords2.Add("and");
            txtQuery.Settings.Keywords2.Add("or");
            txtQuery.Settings.Keywords2.Add("not");

            // Set the comment identifier. For Lua this is two minus-signs after each other (--). 
            // For C++ we would set this property to "//".
            txtQuery.Settings.Comment = "--";
            txtQuery.Settings.Between = "\"";

            // Set the colors that will be used.
            txtQuery.Settings.KeywordColor = Color.Blue;
            txtQuery.Settings.KeywordColor2 = Color.Gray;
            txtQuery.Settings.CommentColor = Color.Green;
            txtQuery.Settings.StringColor = Color.Black;
            txtQuery.Settings.IntegerColor = Color.DarkOrchid;//DarkLayGray

            // Let's not process strings and integers.
            txtQuery.Settings.EnableStrings = false;
            //txtQuery.Settings.EnableIntegers = false;

            // Let's make the settings we just set valid by compiling
            // the keywords to a regular expression.
            txtQuery.CompileKeywords();

            // LUpdate the syntax highlighting.
            txtQuery.ProcessAllLines();
        }

        public static String Standard(String query)
        {
            String s = query;
            String result = "";
            int i = 0;
            while (s.Length > 0)
            {

            }
            return result;
        }

        public static String StandardizeQuery(String query)
        {
            try
            {//The query text has been already cut spaces at the end and at the last string.
                String result = String.Empty;
                for (int i = 0; i < query.Length; i++)
                {
                    if (query[i] == ' ')//(select *    from abc where a=b     and       b=h )=> select * from abc where a=b and b=h
                    {
                        if (query[i - 1] != ' ')//get the single space
                        {
                            result += query[i];
                        }
                    }
                    else
                        result += query[i];
                }

                result = result.Replace("\n", "");
                result = result.Replace("'", "");
                result = result.Replace("<>", "!=");
                result = result.Replace("->", "→");

                return result.ToLower();
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.Message);
            }

            return "";
        }
        public static String CheckSyntaxfirst(String query)// Query must be standarded and remain single space between specify word
        {
            String message = "";
            if (!query.Contains("select"))// Need contains select
                return message = "Query is missing 'select' structure!";

            if (!query.Contains("from"))// Need to contains from
                return message = "Query is missing 'from' structure!";

            int select = query.IndexOf("select");
            int mark = query.IndexOf("*");
            int from = query.IndexOf("from");
            if ((!query.Contains("where")) || (!query.Contains("natural join")) || (!query.Contains("except")) || (!query.Contains("intersect")) || (!query.Contains("union")) || (!query.Contains("descartes")) || (!query.Contains("right join")) || (!query.Contains("left join")) || (!query.Contains("union all")))
            {
                if (query.Substring(from + 4).Trim() == "")// Missing relation after 'from'
                    return message = "Incorrect syntax near 'from': missing relation.";
            }
            else// Check syntax of condition: where (age="young" not weight>=45 or height>150) and height>155
            {
                if (query.Contains("where"))
                {
                    int where = query.IndexOf("where");

                    if (where != query.LastIndexOf("where"))// Where must be unique
                        return message = "Not support multi condition with 'where'!";

                    if (query.Substring(where + 5).Trim() == "")// Missing condition after 'where'
                        return message = "Incorrect syntax near 'where': missing condition.";

                    if (query.Contains("and"))
                    {
                        if (where + 6 == query.IndexOf("and"))
                            return message = "Incorrect syntax near 'where': 'and' does not at the begin of condition.";
                        int i = query.IndexOf("and");
                        if (query[i - 1] == '\"' || query[i + 3] == '\"')
                            return message = "Incorrect syntax near 'and': missing space with '\"'";
                    }
                    if (query.Contains("or"))
                    {
                        if (where + 6 == query.IndexOf("or"))
                            return message = "Incorrect syntax near 'where': 'or' does not at the begin of condition.";
                        int i = query.IndexOf("or");
                        if (query[i - 1] == '\"' || query[i + 2] == '\"')
                            return message = "Incorrect syntax near 'or': missing space with '\"'";
                    }
                    if (query.Contains(" not "))
                    {
                        if (where + 6 == query.IndexOf("not"))
                            return message = "Incorrect syntax near 'where': 'not' does not at the begin of condition.";
                        int i = query.IndexOf("not");
                        if (query[i - 1] == '\"' || query[i + 3] == '\"')
                            return message = "Incorrect syntax near 'not': missing space with '\"'";
                    }

                    String m = CheckNested(query);
                    if (m != "")
                        return m;

                    m = CheckLogic(query);
                    if (m != "")
                        return m;
                    m = CheckQuote(query);
                    if (m != "")
                        return m;

                    m = CheckFuzzySet(query);
                    if (m != "")
                        return m;
                    m = CaseCheckFuzzySet(query, "->", 2);
                    if (m != "")
                        return m;
                }
                if (query.Contains("natural join"))
                {
                    int join = query.IndexOf("natural join");

                    if (join != query.LastIndexOf("natural join"))// Where must be unique
                        return message = "Not support multi condition with 'natural join'!";

                    if (query.Substring(join + 13).Trim() == "")// Missing condition after 'where'
                        return message = "Incorrect syntax near 'natural join': missing condition.";
                }
                if (query.Contains("left join"))
                {
                    int join = query.IndexOf("left join");

                    if (join != query.LastIndexOf("left join"))// Where must be unique
                        return message = "Not support multi condition with 'left join'!";

                    if (query.Substring(join + 10).Trim() == "")// Missing condition after 'where'
                        return message = "Incorrect syntax near 'left join': missing condition.";
                }
                if (query.Contains("right join"))
                {
                    int join = query.IndexOf("right join");

                    if (join != query.LastIndexOf("right join"))// Where must be unique
                        return message = "Not support multi condition with 'right join'!";

                    if (query.Substring(join + 11).Trim() == "")// Missing condition after 'where'
                        return message = "Incorrect syntax near 'right join': missing condition.";
                }
                if (query.Contains("except"))
                {
                    int join = query.IndexOf("except");

                    if (join != query.LastIndexOf("except"))// Where must be unique
                        return message = "Not support multi condition with 'except'!";

                    if (query.Substring(join + 7).Trim() == "")// Missing condition after 'where'
                        return message = "Incorrect syntax near 'except': missing condition.";

                }
                if (query.Contains("descartes"))
                {
                    int join = query.IndexOf("descartes");

                    if (join != query.LastIndexOf("descartes"))// Where must be unique
                        return message = "Not support multi condition with 'descartes'!";

                    if (query.Substring(join + 10).Trim() == "")// Missing condition after 'where'
                        return message = "Incorrect syntax near 'descartes': missing condition.";

                }
                if (query.Contains("union") && (!query.Contains("union all")))
                {
                    int join = query.IndexOf("union");

                    if (join != query.LastIndexOf("union"))// Where must be unique
                        return message = "Not support multi condition with 'union'!";

                    if (query.Substring(join + 6).Trim() == "")// Missing condition after 'where'
                        return message = "Incorrect syntax near 'union': missing condition.";

                }
                if (query.Contains("union all"))
                {
                    int join = query.IndexOf("union all");

                    if (join != query.LastIndexOf("union all"))// Where must be unique
                        return message = "Not support multi condition with 'union'!";

                    if (query.Substring(join + 10).Trim() == "")// Missing condition after 'where'
                        return message = "Incorrect syntax near 'union': missing condition.";

                }
                if (query.Contains("intersect"))
                {
                    int join = query.IndexOf("intersect");

                    if (join != query.LastIndexOf("intersect"))// Where must be unique
                        return message = "Not support multi condition with 'intersect'!";

                    if (query.Substring(join + 10).Trim() == "")// Missing condition after 'where'
                        return message = "Incorrect syntax near 'intersect': missing condition.";

                }


            }

            return message;
        }
        public static String CheckSyntax(String query)// Query must be standarded and remain single space between specify word
        {
            String message = "";
            if (!query.Contains("select"))// Need contains select
                return message = "Query is missing 'select' structure!";

            if (!query.Contains("from"))// Need to contains from
                return message = "Query is missing 'from' structure!";

            int select = query.IndexOf("select");
            int mark = query.IndexOf("*");
            int from = query.IndexOf("from");
            if (select != query.LastIndexOf("select"))// Select must be unique
                return message = "Not support multi 'select' in " + "' " + query + " '";

            if (mark != query.LastIndexOf("*"))// * must be unique
                return message = "Not support multi '*'in " + "' " + query + " '";

            if (from != query.LastIndexOf("from"))// From must be unique
                return message = "Not support multi 'from' in " + "' " + query + " '";

            if ((!query.Contains("where")) || (!query.Contains("natural join")) || (!query.Contains("except") || (!query.Contains("intersect"))) || (!query.Contains("descartes")) || (!query.Contains("union all")) || (!query.Contains("right join")) || (!query.Contains("left join")) || (!query.Contains("union")))
            {
                if (query.Substring(from + 4).Trim() == "")// Missing relation after 'from'
                    return message = "Incorrect syntax near 'from': missing relation.";
            }
            else// Check syntax of condition: where (age="young" not weight>=45 or height>150) and height>155
            {
                if (query.Contains("where"))
                {
                    int where = query.IndexOf("where");

                    if (where != query.LastIndexOf("where"))// Where must be unique
                        return message = "Not support multi condition with 'where'!";

                    if (query.Substring(where + 5).Trim() == "")// Missing condition after 'where'
                        return message = "Incorrect syntax near 'where': missing condition.";

                    if (query.Contains("and"))
                    {
                        if (where + 6 == query.IndexOf("and"))
                            return message = "Incorrect syntax near 'where': 'and' does not at the begin of condition.";
                        int i = query.IndexOf("and");
                        if (query[i - 1] == '\"' || query[i + 3] == '\"')
                            return message = "Incorrect syntax near 'and': missing space with '\"'";
                    }
                    if (query.Contains("or"))
                    {
                        if (where + 6 == query.IndexOf("or"))
                            return message = "Incorrect syntax near 'where': 'or' does not at the begin of condition.";
                        int i = query.IndexOf("or");
                        if (query[i - 1] == '\"' || query[i + 2] == '\"')
                            return message = "Incorrect syntax near 'or': missing space with '\"'";
                    }
                    if (query.Contains(" not "))
                    {
                        if (where + 6 == query.IndexOf("not"))
                            return message = "Incorrect syntax near 'where': 'not' does not at the begin of condition.";
                        int i = query.IndexOf("not");
                        if (query[i - 1] == '\"' || query[i + 3] == '\"')
                            return message = "Incorrect syntax near 'not': missing space with '\"'";
                    }

                    String m = CheckNested(query);
                    if (m != "")
                        return m;

                    m = CheckLogic(query);
                    if (m != "")
                        return m;
                    m = CheckQuote(query);
                    if (m != "")
                        return m;

                    m = CheckFuzzySet(query);
                    if (m != "")
                        return m;
                    m = CaseCheckFuzzySet(query, "->", 2);
                    if (m != "")
                        return m;
                }

            }

            return message;
        }

        #endregion
        #region 5. Privates
        private static String CheckLogic(String query)
        {
            String message = "";
            String select = query.Substring(0, query.IndexOf("where"));

            if (select.Contains(" and ") || select.Contains(" or ") || select.Contains(" not ") || select.Contains("(") || select.Contains(")") || select.Contains("\""))
                return message = "Select clause do not allow 'and', 'or', 'not', '(', ')' and '\"'.";

            if (query.Contains("()"))
                return message = "Incorrect syntax '()'.";
            int i = 0, j = 0;
            while (i < query.Length)
            {
                if (query[i] == ')' && i < query.Length - 1)
                {
                    j = i + 1;
                    while (query[j] != '(') j++;
                    String s = query.Substring(i + 1, j - i - 1);
                    int count = 0;
                    if (s == " and ") count++;
                    if (s == " or ") count++;
                    if (s == " not ") count++;
                    if (count == 0)
                    {
                        i = j + 1;
                        return message = "Missing logicality between two expression.";
                    }
                }
                i++;
            }

            //Check space surround logicality

            return message;
        }
        private static String CheckNested(String query)
        {
            String message = "";
            int i = 1, j = 0;
            while (i < query.Length - 1)
            {
                if (query[i] == '(')
                {
                    if (query[i - 1] != ' ')
                        return message = "Incorrect syntax near '(': missing space";
                    if (!query.Substring(i).Contains(")"))
                        return message = "Missing close parenthesis '('.";

                    j = i + 1;
                    while (j < query.Length)
                    {
                        if (query[j] == ')')
                        {
                            if (j < query.Length - 1 && query[j + 1] != ' ')
                                return message = "Incorrect syntax near ')': missing space";

                            i = j + 1;
                            break;
                        }
                        if (query[j] == '(')
                            return message = "Do not support nested parenthesis near the key word '('.";
                        j++;
                    }

                }

                i++;
            }

            i = j = query.Length - 1;
            while (i > 0)
            {
                if (query[i] == ')')
                {
                    j = i - 1;
                    while (j > 1)
                    {
                        if (query[j] == ')')
                            if (!query.Substring(j + 1, i - j).Contains("("))
                            {
                                i = j;
                                return message = "Missing open parenthesis '('.";
                            }
                        j--;
                    }
                }

                i--;
            }
            return message;
        }

        private static String CheckQuote(String query)
        {
            string message = "";
            int index = query.IndexOf("where");

            //Remove space next to operator
            String str = query.Substring(index);
            for (int i = 1; i < str.Length - 1; i++)
            {
                if (str[i] == '>' || str[i] == '<' || str[i] == '!' || str[i] == '=')
                {
                    if (str[i - 1] == ' ')
                        str = str.Remove(i - 1, 1);
                    if (str[i + 1] == ' ')
                        str = str.Remove(i + 1, 1);
                }
            }
            // Check quote
            int j = 1, k = 0;
            while (j < str.Length - 1)
            {
                if (str[j] == '\"')
                {
                    if (Count(str[j - 1].ToString()) == 0)// Mean " is open quote in the left operator
                    {
                        k = j + 1;
                        while (k < str.Length)
                        {
                            if (Count(str[k].ToString()) > 0)
                            {
                                if (str[k - 1] != '\"')
                                    return message = "Missing close quote '\"'";
                                else
                                {
                                    j = k;
                                    break;
                                }
                            }
                            else if (k == str.Length - 1)
                            {
                                return message = "Incorrect near '" + str.Substring(j - 1) + "'.";
                            }
                            k++;
                        }
                    }
                    else// Mean " is open quote in the right of operator
                    {
                        String s = str.Substring(j + 1);
                        int a = int.MaxValue, b = int.MaxValue, c = int.MaxValue;
                        if (s.Contains(" and "))
                            a = s.IndexOf(" and ");
                        if (s.Contains(" or "))
                            b = s.IndexOf(" or ");
                        if (s.Contains(" not "))
                            c = s.IndexOf(" not ");
                        int m = Math.Min(Math.Min(a, b), c);

                        if (s.Contains("\""))
                        {
                            if (s.IndexOf("\"") > m - 1)
                                return message = "Missing close quote.";
                            else
                            {
                                j = m;
                                break;
                            }
                        }
                        else
                            return message = "Missing close quote.";

                    }
                }

                j++;
            }

            if (str[str.Length - 1] == '\"')
            {
                j = k = str.Length - 2;
                while (k > 1)
                {
                    if (str[k] == '\"')
                    {
                        if (Count(str[k - 1].ToString()) == 0)
                            message = "Incorrect syntax '" + str.Substring(k) + "': missing string compare and close quote.";
                        break;
                    }

                    k--;
                }
            }
            // Check String


            return message;
        }

        private static int Count(String c)
        {
            int count = 0;
            if (c.Contains(">")) count++;
            if (c.Contains("<")) count++;
            if (c.Contains("=")) count++;
            if (c.Contains("!")) count++;
            if (c.Contains("→")) count++;

            return count;
        }

        private static String CheckObject(String query, FdbEntity fdb)
        {
            String message = "";
            int i = query.IndexOf("from" + 5);
            int j = query.IndexOf("where");
            String relation = query.Substring(i, j);
            int c = 0;
            foreach (var item in fdb.Relations)
            {
                if (item.RelationName == relation)
                {
                    c++;
                    break;
                }
            }
            if (c == 0)
                return message = "Invalid object relation name '" + relation + "'";

            return message;
        }

        private static String CheckFuzzySet(String query)
        {
            if (query.Contains("->"))
                return CaseCheckFuzzySet(query, "->", 2);
            else if (query.Contains("→"))
                return CaseCheckFuzzySet(query, "→", 1);
            else
                return CaseCheckFuzzySet(query, "→", 2);
        }

        private static String CaseCheckFuzzySet(String query, String key, int l)
        {
            String message = "";
            if (query.Contains(key))
            {
                int k = query.IndexOf(key) + l;
                if (query[k].ToString() == "\"")
                    return message = "The name of fuzzy set must be outside of quotes";
                else
                {
                    for (int i = 0; i < query.Length; i++)
                    {
                        if (query[i] == ' ')
                        {
                            if (query[i + 1] == '"')
                                return message = "Incorrect syntax near \". Fuzzy set must be outside of quotes";
                        }
                    }
                }
            }

            return message;
        }

        #endregion
    }
}
