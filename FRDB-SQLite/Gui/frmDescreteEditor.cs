using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using FRDB_SQLite;
using FRDB_SQLite;
using System.IO;
using FRDB_SQLite;

namespace FRDB_SQLite.Gui
{
    public partial class frmDescreteEditor : DevExpress.XtraEditors.XtraForm
    {
        public frmDescreteEditor()
        {
            InitializeComponent();
            InitializeObject();
            AddRow();
        }

        public frmDescreteEditor(String name, List<Double> values, List<Double> memberships)
        {
            InitializeComponent();
            this.LanguisticLabel = name;
            this.Values = values;
            this.Memberships = memberships;
            AddRow();
        }

        private DataTable dt = new DataTable();
        public String LanguisticLabel { get; set; }
        public List<Double> Values { get; set; }
        public List<Double> Memberships { get; set; }

        #region 1. Process Data

        private void InitializeObject()
        {
            this.LanguisticLabel = String.Empty;
            this.Values = new List<double>();
            this.Memberships = new List<double>();
        }

        private void AddRow()
        {
            dt.Columns.Add("values", typeof(Double));
            dt.Columns.Add("memberships", typeof(Double));
            
            txtLinguistic.Text = this.LanguisticLabel;

            if (this.Values.Count == 0)
            {
                DataRow row = dt.NewRow();
                row[0] = 0;
                row[1] = 0;
                dt.Rows.Add(row);
            }
            else
            {
                for (int i = 0; i < Values.Count; i++)//Bescause Values and Memberships is always the same elements
                {
                    DataRow row = dt.NewRow();
                    row[0] = Values[i];
                    row[1] = Memberships[i];
                    dt.Rows.Add(row);
                }
            }

            gridControl1.DataSource = dt;
        }

        private DiscreteFuzzySetBLL GetDataRows()
        {
            DiscreteFuzzySetBLL result = new DiscreteFuzzySetBLL();
            result.FuzzySetName = this.txtLinguistic.Text.Trim();

            for (int i = 0; i < gridView1.DataRowCount; i++)
            {
                result.AddPoint(Convert.ToDouble(gridView1.GetRowCellValue(i, "values").ToString()),
                                Convert.ToDouble(gridView1.GetRowCellValue(i, "memberships").ToString()));
            }

            return result;

        }

        private List<String> GetDataRows1()
        {
            List<String> result = new List<String>();
            result.Add(this.txtLinguistic.Text.Trim());//First element is Name
            String values = "";
            String memberships = "";

            for (int i = 0; i < gridView1.DataRowCount; i++)
            {
                values += gridView1.GetRowCellValue(i, "values").ToString() + ",";
                memberships += gridView1.GetRowCellValue(i, "memberships").ToString() + ",";
            }

            values = values.Remove(values.LastIndexOf(","));
            memberships = memberships.Remove(memberships.LastIndexOf(","));
            result.Add(values);
            result.Add(memberships);

            return result;
        }

        #endregion

        #region 2. Button Click

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            if (!IsValuesNull() || !IsData())
            {
                return;
            }

            Double endValue = Convert.ToDouble(gridView1.GetRowCellValue(gridView1.DataRowCount -1, "values"));
            DataRow dr;
            dr = dt.NewRow();
            dr[0] = endValue + 1;
            dr[1] = 0;
            dt.Rows.Add(dr);
            
            gridControl1.DataSource = dt;
        }

        private void simpleButton2_Click(object sender, EventArgs e)
        {
            if (dt.Rows.Count > 1)
            {
                dt.Rows.RemoveAt(dt.Rows.Count - 1);
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!IsValuesNull() || !IsData())
            {
                return;
            }

            String path = Directory.GetCurrentDirectory() + @"\lib\";
            //DiscreteFuzzySetBLL newDisFs = GetDataRows();
            List<String> list = GetDataRows1();
            //if (newDisFs.Update() == 1)
            if (new FuzzyProcess().UpdateFS(path, list.GetRange(1,2), list[0]+".disFS") == 1)
            {
                MessageBox.Show("Save Fuzzy Set DONE!");
            }
            else
            {
                frmRunAsAdministrator frm = new frmRunAsAdministrator();
                frm.ShowDialog();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region 3. Check values

        private Boolean IsValuesNull()
        { 
            if (txtLinguistic.Text.Trim() == "")
            {
                MessageBox.Show("Fuzzy Set Name is missing!");
                return false;
            }
            string path1 = Directory.GetCurrentDirectory() + @"\lib\";
            List<DisFS> list = new FuzzyProcess().GenerateAllDisFS(path1);
        
            
            for (int i = 0; i < gridView1.DataRowCount; i++)
            {
                if (gridView1.GetRowCellValue(i, "values").ToString().Trim() == "" ||
                    gridView1.GetRowCellValue(i, "memberships").ToString().Trim() == "")
                {
                    MessageBox.Show("Missing values on row!\n(Remove row or fill values in row)");
                    return false;
                }
            }

            return true;
        }

        private Boolean IsData()
        {
            for (int i = 0; i < gridView1.DataRowCount; i++)
            {
                if (gridView1.GetRowCellValue(i, "values").ToString() != "" &&
                    gridView1.GetRowCellValue(i, "memberships").ToString() != "")
                {
                    //Check values
                    for (int j = i + 1; j < gridView1.DataRowCount ; j++ )
                    {
                        if (Convert.ToDouble(gridView1.GetRowCellValue(j, "values")) ==
                            (Convert.ToDouble(gridView1.GetRowCellValue(i, "values"))))
                        {
                            MessageBox.Show("Value \"" + gridView1.GetRowCellValue(j, "values") + "\" is a key in \"values column\", it doesn't equal to others key!");
                            return false;
                        }
                        
                    }
                   
                    for (int j = i - 1; j > 0; j--)
                    {
                        if (Convert.ToDouble(gridView1.GetRowCellValue(j, "values")) ==
                            (Convert.ToDouble(gridView1.GetRowCellValue(i, "values"))))
                        {
                            MessageBox.Show("Value \"" + gridView1.GetRowCellValue(j, "values") + "\" is a key in \"values column\", it doesn't equal to others key!");
                            return false;
                        }
                    }

                    //Check memberships
                    if (Convert.ToDouble(gridView1.GetRowCellValue(i, "memberships")) < 0 ||
                        Convert.ToDouble(gridView1.GetRowCellValue(i, "memberships")) > 1)
                    {
                        MessageBox.Show("Some values of membership weren't correct data\n(Membership values must be in [0, 1]!");
                        return false;
                    }
                }
            }
            return true;
        }
        #endregion
    }
            
}
