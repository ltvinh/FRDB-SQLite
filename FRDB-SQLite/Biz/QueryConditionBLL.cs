using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FRDB_SQLite;
using FRDB_SQLite;
using System.IO;

namespace FRDB_SQLite
{
    public class QueryConditionBLL
    {
        #region 1. Fields

        private List<FzAttributeEntity> _attributes = new List<FzAttributeEntity>();
        private Double _uRelation;
        private List<String> _memberships;

        private FzTupleEntity _resultTuple;
        private List<FzRelationEntity> _selectedRelations;
        private List<Item> _itemConditions;
        private String _errorMessage;
        #endregion

        #region 2. Properties
        public FzTupleEntity ResultTuple
        {
            get { return _resultTuple; }
            set
            {
                FzTupleEntity newTuple = new FzTupleEntity(value);
                this._resultTuple = newTuple;
            }
        }

        public List<FzRelationEntity> SelectedRelations
        {
            get { return _selectedRelations; }
            set { _selectedRelations = value; }
        }

        public List<Item> ItemConditions
        {
            get { return _itemConditions; }
            set
            {
                foreach (var items in value)
                {
                    Item item = new Item() { elements = items.elements, nextLogic = items.nextLogic };
                    this._itemConditions.Add(item);
                }
            }
        }

        public String ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; }
        }

        #endregion

        #region 3. Contructors
       
        public QueryConditionBLL() { }

        public QueryConditionBLL(List<Item> items, List<FzRelationEntity> sets)
        {
            //this._resultTuple = new FzTupleEntity();
            this._selectedRelations = sets;
            this._itemConditions = items;
            this._errorMessage = "";

            //this._memberships = new List<string>();
            this._uRelation = Double.MaxValue;
            this._attributes = sets[0].Scheme.Attributes;
        }

        #endregion

        #region 4. Publics
        /// <summary>
        /// 
        /// </summary>
        public Boolean Satisfy(List<Item> list, FzTupleEntity tuple)
        {
            this._resultTuple = new FzTupleEntity() { ValuesOnPerRow = tuple.ValuesOnPerRow };//select * from rpatient where age <> "old"
            _uRelation = Convert.ToDouble(tuple.ValuesOnPerRow[tuple.ValuesOnPerRow.Count - 1]);
            this._memberships = new List<string>();

            String logicText = String.Empty;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].elements.Count == 3)// single expression: age='young'.
                {
                    if (SatisfyItem(list[i].elements, tuple, i - 1))
                        logicText += "1" + list[i].nextLogic;
                    else
                        logicText += "0" + list[i].nextLogic;
                }
                else// Multiple expression: weight='heavy' not height>165
                {
                    List<Item> subItems = CreateSubItems(list[i].elements);
                    Boolean end = Satisfy(subItems, tuple);
                    if (end)
                        logicText += "1" + list[i].nextLogic;
                    else
                        logicText += "0" + list[i].nextLogic;
                }
            }

            logicText = ReplaceLogicality(logicText);
            _resultTuple.ValuesOnPerRow[_resultTuple.ValuesOnPerRow.Count - 1] = UpdateMembership(_memberships);
            return CalulateLogic(logicText);
        }
        #endregion

        #region 5. Privates
        /// <summary>
        /// 
        /// </summary>
        private Double UpdateMembership(List<String> memberships)
        {
            if (memberships.Count == 0) return Convert.ToDouble(_resultTuple.ValuesOnPerRow[_resultTuple.ValuesOnPerRow.Count - 1]);

            Double result = Convert.ToDouble(memberships[0]);
            while (memberships.Count > 1)//0.9 and 0.8 or 0.5
            {
                Double v1 = Convert.ToDouble(memberships[0]);
                Double v2 = Convert.ToDouble(memberships[2]);
                switch (memberships[1])
                {
                    case " and ": result = Math.Min(v1, v2); break;
                    case " or ": result = Math.Max(v1, v2); break;
                    case " not ": result = Math.Min(v1, 1 - v2); break;
                }
                memberships.RemoveRange(0, 2);
                memberships[0] = result.ToString();
            }
            return result;
        }

        private List<Item> CreateSubItems(List<String> itemCondition)
        {
            List<String> items = new List<string>();
            foreach (var item in itemCondition)
            {
                items.Add(item);
            }
            List<Item> subItems = new List<Item>();
            while (items.Count > 0)
            {
                Item item = new Item();
                item.elements.Add(items[0]);
                item.elements.Add(items[1]);
                item.elements.Add(items[2]);
                if (items.Count >= 4)
                {
                    item.nextLogic = items[3];
                    subItems.Add(item);
                    items.RemoveRange(0, 4);
                }
                else// No need to add because the nextLogic default is empty
                {
                    subItems.Add(item);
                    items.RemoveRange(0, 3);
                }
            }

            return subItems;
        }

        private String ReplaceLogicality(String logicText)
        {
            logicText = logicText.Replace(" and ", "&");
            logicText = logicText.Replace(" or ", "|");
            logicText = logicText.Replace(" not ", "!");
            return logicText;
        }

        /// <summary>
        /// 
        /// </summary>
        public Boolean CalulateLogic(String l)
        {
            try
            {
                string bit = "";
                while (l.Length > 1)
                {
                    bool v1 = (l[0].CompareTo('1') == 0) ? true : false;
                    bool v2 = (l[2].CompareTo('1') == 0) ? true : false;

                    switch (l[1])
                    {
                        case '&': bit = ((v1 && v2) ? "1" : "0");
                            break;
                        case '|': bit = ((v1 || v2) ? "1" : "0");
                            break;
                        case '!': bit = ((v1 != v2) ? "1" : "0");
                            break;
                    }

                    l = l.Remove(0, 3);//false && false != false && true || true;
                    l = bit + l;
                }
            }
            catch (Exception ex)
            {
                this._errorMessage = ex.Message;
            }
            return (l.CompareTo("1") == 0) ? true : false;
        }

        /// <summary>
        ///  The variable i for getting the previous logicality
        /// </summary>
        //private Boolean SatisfyItem(List<String> itemCondition, FzTupleEntity tuple, int i)
        //{
        //    int indexAttr = Convert.ToInt32(itemCondition[0]);
        //    String dataType = this._attributes[indexAttr].DataType.DataType;
        //    Object value = tuple.ValuesOnPerRow[indexAttr];//we don't know the data type of value
        //    int count = 0;
        //    String fs = itemCondition[2];
        //        //fs = itemCondition[2].Substring(1, itemCondition[2].Length - 2);;
        //    ContinuousFuzzySetBLL conFS = null;
        //    DiscreteFuzzySetBLL disFS = null;

        //    if (itemCondition[1] == "->" || itemCondition[1] == "→")
        //    {
        //        //fs = fs.Substring(1, fs.Length - 2);
        //        conFS = new ContinuousFuzzySetBLL().GetByName(fs);
        //        disFS = new DiscreteFuzzySetBLL().GetByName(fs);//2 is value of user input
        //    }
        //    if (conFS != null)//continuous fuzzy set is priorer than discrete fuzzy set
        //    {
        //        //itemCondition[1] is operator, uValue is the membership of the value on current cell for the selected fuzzy set
        //        Double uValue = FuzzyCompare(Convert.ToDouble(value), conFS, itemCondition[1]);
        //        uValue = Math.Min(uValue, _uRelation);//Update the min value
        //        if (uValue != 0)
        //        {
        //            if (i != -1 && _memberships.Count > 0)// Getting previous logicality
        //                _memberships.Add(_itemConditions[i].nextLogic);
        //            _memberships.Add(uValue.ToString());
        //            count++;
        //        }
        //    }
        //    if (disFS != null && conFS == null)
        //    {
        //        Double uValue = FuzzyCompare(Convert.ToDouble(value), disFS, itemCondition[1]);
        //        uValue = Math.Min(uValue, _uRelation);//Update the min value
        //        if (uValue != 0)
        //        {
        //            if (i != -1 && _memberships.Count > 0)// Getting previous logicality
        //                _memberships.Add(_itemConditions[i].nextLogic);
        //            _memberships.Add(uValue.ToString());
        //            count++;
        //        }
        //    }
        //    if (disFS == null && conFS == null)
        //    {
        //        //if (fs.Contains("\""))
        //        //    fs = fs.Substring(1, fs.Length - 2);
        //        if (ObjectCompare(value, fs, itemCondition[1], dataType))
        //        {
        //            count++;
        //        }
        //    }

        //    if (count == 1)//it mean the tuple is satisfied with all the compare operative
        //    {
        //        return true;
        //    }

        //    return false;
        //}

        private Boolean SatisfyItem(List<String> itemCondition, FzTupleEntity tuple, int i)
        {
            int indexAttr = Convert.ToInt32(itemCondition[0]);
            String dataType = this._attributes[indexAttr].DataType.DataType;
            Object value = tuple.ValuesOnPerRow[indexAttr];//we don't know the data type of value
            int count = 0;
            String fs = itemCondition[2];
            //fs = itemCondition[2].Substring(1, itemCondition[2].Length - 2);;
            ConFS conFS = null;
            DisFS disFS = null;
            string path = Directory.GetCurrentDirectory() + @"\lib\";
            if (itemCondition[1] == "->" || itemCondition[1] == "→")
            {
                //fs = fs.Substring(1, fs.Length - 2);
                conFS = new FuzzyProcess().ReadEachConFS(path + fs + ".conFS");
                disFS = new FuzzyProcess().ReadEachDisFS(path + fs + ".disFS");//2 is value of user input
            }
            if (conFS != null)//continuous fuzzy set is priorer than discrete fuzzy set
            {
                //itemCondition[1] is operator, uValue is the membership of the value on current cell for the selected fuzzy set
                Double uValue = FuzzyCompare(Convert.ToDouble(value), conFS, itemCondition[1]);
                uValue = Math.Min(uValue, _uRelation);//Update the min value
                if (uValue != 0)
                {
                    if (i != -1 && _memberships.Count > 0)// Getting previous logicality
                        _memberships.Add(_itemConditions[i].nextLogic);
                    _memberships.Add(uValue.ToString());
                    count++;
                }
            }
            if (disFS != null && conFS == null)
            {
                Double uValue = FuzzyCompare(Convert.ToDouble(value), disFS, itemCondition[1]);
                uValue = Math.Min(uValue, _uRelation);//Update the min value
                if (uValue != 0)
                {
                    if (i != -1 && _memberships.Count > 0)// Getting previous logicality
                        _memberships.Add(_itemConditions[i].nextLogic);
                    _memberships.Add(uValue.ToString());
                    count++;
                }
            }
            if (disFS == null && conFS == null)
            {
                //if (fs.Contains("\""))
                //    fs = fs.Substring(1, fs.Length - 2);
                if (ObjectCompare(value, fs, itemCondition[1], dataType))
                {
                    count++;
                }
            }

            if (count == 1)//it mean the tuple is satisfied with all the compare operative
            {
                return true;
            }

            return false;
        }


        private Boolean StringCompare(String a, String b, String opr)
        {
            a = "\"" + a + "\"";
            switch (opr)
            {
                case "=": return (a.CompareTo(b) == 0);
                case "!=": return (a.CompareTo(b) != 0);
            }

            return false;
        }

        private Boolean BoolCompare(Boolean a, Boolean b, String opr)
        {
            switch (opr)
            {
                case "=": return (a == b);
                case "!=": return (a != b);
            }

            return false;
        }

        private Boolean DoubleCompare(Double a, Double b, String opr)
        {
            switch (opr)
            {
                case "<": return (a < b);
                case ">": return (a > b);
                case "<=": return (a <= b);
                case ">=": return (a >= b);
                case "=": return (Math.Abs(a - b) < 0.001);
                case "!=": return (Math.Abs(a - b) > 0.001);
            }

            return false;
        }

        private Boolean IntCompare(int a, int b, String opr)
        {
            switch (opr)
            {
                case "<": return (a < b);
                case ">": return (a > b);
                case "<=": return (a <= b);
                case ">=": return (a >= b);
                case "=": return (a == b);
                case "!=": return (a != b);
            }

            return false;
        }


        #region Fuzzy Set
        /// <summary>
        /// Get the dataType of attribute index
        /// After that convert value in tuple and value of user input to the same data type of attribute
        /// Comparing two values belongs to operator of data type
        /// </summary>
        private Boolean ObjectCompare(Object value, String input, String opr, String type)
        {
            switch (type)
            {
                case "Int16":
                case "Int64":
                case "Int32":
                case "Byte":
                case "Currency": return IntCompare(Convert.ToInt32(value), Convert.ToInt32(input), opr);
                case "String":
                case "DateTime":
                case "UserDefined":
                case "Binary": return StringCompare(value.ToString().ToLower(), input.ToLower(), opr);
                case "Decimal":
                case "Single":
                case "Double": return DoubleCompare(Convert.ToDouble(value), Convert.ToDouble(input), opr);
                case "Boolean": return BoolCompare(Convert.ToBoolean(value), Convert.ToBoolean(input), opr);
            }

            return false;
        }

        //private Double FuzzyCompare(Double value, ContinuousFuzzySetBLL set, String opr)
        //{
        //    Double result = 0;
        //    Double membership = set.GetMembershipAt(value);

        //    switch (opr)
        //    {
        //        case "→":
        //            if (value >= set.Bottom_Left && value <= set.Bottom_Right)
        //                result = membership;
        //            return result;
        //        case "<"://
        //            if (value < set.Bottom_Left)
        //                result = 1;
        //            return result;

        //        case ">":
        //            if (value > set.Bottom_Right)
        //                result = 1;
        //            return result;

        //        case "<=":
        //            if (value < set.Bottom_Right)
        //                result = 1;//select 
        //            if (value >= set.Bottom_Left && value <= set.Bottom_Right)
        //                result = membership;
        //            return result;

        //        case ">=":
        //            if (value > set.Bottom_Left)
        //                result = 1;//select 
        //            if (value >= set.Bottom_Left && value <= set.Bottom_Right)
        //                result = membership;
        //            return result;

        //        case "=":
        //            if (value >= set.Bottom_Left && value <= set.Bottom_Right)
        //                result = membership;
        //            return result;

        //        case "!="://No need to get the membership
        //            if (value <= set.Bottom_Left || value >= set.Bottom_Right)
        //                result = 1;//selet the tuple
        //            return result;
        //    }

        //    return result;
        //}
        private Double FuzzyCompare(Double value, ConFS set, String opr)
        {
            Double result = 0;
            Double membership = set.GetMembershipAt(value);

            switch (opr)
            {
                case "→":
                    if (value >= set.Bottom_Left && value <= set.Bottom_Right)
                        result = membership;
                    return result;
                case "<"://
                    if (value < set.Bottom_Left)
                        result = 1;
                    return result;

                case ">":
                    if (value > set.Bottom_Right)
                        result = 1;
                    return result;

                case "<=":
                    if (value < set.Bottom_Right)
                        result = 1;//select 
                    if (value >= set.Bottom_Left && value <= set.Bottom_Right)
                        result = membership;
                    return result;

                case ">=":
                    if (value > set.Bottom_Left)
                        result = 1;//select 
                    if (value >= set.Bottom_Left && value <= set.Bottom_Right)
                        result = membership;
                    return result;

                case "=":
                    if (value >= set.Bottom_Left && value <= set.Bottom_Right)
                        result = membership;
                    return result;

                case "!="://No need to get the membership
                    if (value <= set.Bottom_Left || value >= set.Bottom_Right)
                        result = 1;//selet the tuple
                    return result;
            }

            return result;
        }

        //private Double FuzzyCompare(Double value, DiscreteFuzzySetBLL set, String opr)
        //{
        //    Double result = 0;
        //    Double max = set.GetMaxValue();
        //    Double min = set.GetMinValue();
        //    Double membership = set.GetMembershipAt(value);
        //    Boolean isMember = set.IsMember(value);

        //    switch (opr)
        //    {
        //        case "→":
        //            if (isMember)
        //                result = membership;
        //            return result;
        //        case "<"://
        //            if ( value < min)
        //                result = 1;
        //            return result;

        //        case ">":
        //            if (value > max)
        //                result = 1;
        //            return result;

        //        case "<=":

        //            if (value < min)
        //                result = 1;
        //            if (isMember)
        //                result = membership;
        //            return result;

        //        case ">=":
        //            if (value > max)
        //                result = 1;//select 
        //            if (isMember)
        //                result = membership;
        //            return result;

        //        case "=":
        //            if (isMember)
        //                result = membership;
        //            return result;

        //        case "!="://No need to get the membership
        //            if (!isMember)
        //                result = 1;
        //            return result;
        //    }

        //    return result;
        //}
        private Double FuzzyCompare(Double value, DisFS set, String opr)
        {
            Double result = 0;
            Double max = set.GetMaxValue();
            Double min = set.GetMinValue();
            Double membership = set.GetMembershipAt(value);
            Boolean isMember = set.IsMember(value);

            switch (opr)
            {
                case "→":
                    if (isMember)
                        result = membership;
                    return result;
                case "<"://
                    if (value < min)
                        result = 1;
                    return result;

                case ">":
                    if (value > max)
                        result = 1;
                    return result;

                case "<=":

                    if (value < min)
                        result = 1;
                    if (isMember)
                        result = membership;
                    return result;

                case ">=":
                    if (value > max)
                        result = 1;//select 
                    if (isMember)
                        result = membership;
                    return result;

                case "=":
                    if (isMember)
                        result = membership;
                    return result;

                case "!="://No need to get the membership
                    if (!isMember)
                        result = 1;
                    return result;
            }

            return result;
        }
        #endregion
        #endregion
    }
}
