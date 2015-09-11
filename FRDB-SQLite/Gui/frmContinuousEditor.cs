using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using System.IO;
using FRDB_SQLite;
using FRDB_SQLite;

namespace FRDB_SQLite.Gui
{
    public partial class frmContinuousEditor : DevExpress.XtraEditors.XtraForm
    {
        public frmContinuousEditor()
        {
            InitializeComponent();
            
        }

        public frmContinuousEditor(String name, Double bl, Double tl, Double tr, Double br)
        {
            InitializeComponent();

            txtLinguistic.Text = name;
            txtBottomLeft.Text = bl.ToString();
            txtTopLeft.Text = tl.ToString();
            txtTopRight.Text = tr.ToString();
            txtBottomRight.Text = br.ToString();
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (!CheckNull()) return;
            if (!CheckLogicValue()) return;
            //ContinuousFuzzySetBLL newFS = new ContinuousFuzzySetBLL();
            FuzzyProcess fz = new FuzzyProcess();
            ConFS newFS = new ConFS();
            //newFS.FuzzySetName = txtLinguistic.Text.Trim();
            newFS.Name = txtLinguistic.Text.Trim() + ".conFS";

            String content = txtBottomLeft.Text.Trim();

            string path = Directory.GetCurrentDirectory() + @"\lib\";

            if (txtTopLeft.Text.Trim() == "" && txtTopRight.Text.Trim() != "")
            {
                newFS.Bottom_Left = Convert.ToDouble(txtBottomLeft.Text);
                newFS.Top_Left = newFS.Top_Right = Convert.ToDouble(txtTopRight.Text);
                newFS.Bottom_Right = Convert.ToDouble(txtBottomRight.Text);
                content += "," + txtTopRight.Text.Trim() + "," + txtTopRight.Text.Trim() + "," + txtBottomRight.Text.Trim();
                //if (newFS.Update() == 1)
                if (fz.UpdateFS(path, content, newFS.Name) == 1)
                {
                    MessageBox.Show("Save Fuzzy Set DONE!");
                }
                else 
                {
                    frmRunAsAdministrator frm = new frmRunAsAdministrator();
                    frm.ShowDialog();
                }
            }
            else if (txtTopLeft.Text.Trim() != "" && txtTopRight.Text.Trim() == "")
            {
                newFS.Bottom_Left = Convert.ToDouble(txtBottomLeft.Text);
                newFS.Top_Left = newFS.Top_Right = Convert.ToDouble(txtTopLeft.Text);
                newFS.Bottom_Right = Convert.ToDouble(txtBottomRight.Text);

                content += "," + txtTopLeft.Text.Trim() + "," + txtTopLeft.Text.Trim() + "," + txtBottomRight.Text.Trim();
                //if (newFS.Update() == 1)
                if (fz.UpdateFS(path, content, newFS.Name) == 1)
                {
                    MessageBox.Show("Save Fuzzy Set DONE!");
                }
                else
                {
                    frmRunAsAdministrator frm = new frmRunAsAdministrator();
                    frm.ShowDialog();
                }
            }
            else
            {
                newFS.Bottom_Left = Convert.ToDouble(txtBottomLeft.Text);
                newFS.Top_Left = Convert.ToDouble(txtTopLeft.Text);
                newFS.Top_Right = Convert.ToDouble(txtTopRight.Text);
                newFS.Bottom_Right = Convert.ToDouble(txtBottomRight.Text);

                content += "," + txtTopLeft.Text.Trim() + "," + txtTopRight.Text.Trim() + "," + txtBottomRight.Text.Trim();
                //if (newFS.Update() == 1)
                if (fz.UpdateFS(path, content, newFS.Name) == 1)
                {
                    MessageBox.Show("Save Fuzzy Set DONE!");
                }
                else
                {
                    frmRunAsAdministrator frm = new frmRunAsAdministrator();
                    frm.ShowDialog();
                }
            }
        }

        private bool CheckNull()
        {
            if (txtLinguistic.Text.Trim() == "")
            {
                MessageBox.Show("The linguistic does not empty!");
                return false;
            }
            string path1 = Directory.GetCurrentDirectory() + @"\lib\";
            List<DisFS> list = new FuzzyProcess().GenerateAllDisFS(path1);
           
            if (txtBottomLeft.Text.Trim() == "" || txtBottomLeft.Text.Trim() == null)
            {
                MessageBox.Show("Bottom-Left is empty!");
                return false;
            }
            
            if ((txtTopLeft.Text.Trim() == "" && txtTopRight.Text == ""))
            {
                MessageBox.Show("It' just allow one of Top-Left and Top-Right null!");
                return false;
            }

            if (txtBottomRight.Text.Trim() == "" || txtBottomRight.Text.Trim() == null)
            {
                MessageBox.Show("Bottom-Right is empty!");
                return false;
            }

            return true;
        }

        private Boolean CheckLogicValue()
        {

            Double bl = 0; if (txtBottomLeft.Text != "") bl = Double.Parse(txtBottomLeft.Text);
            Double tl = 0; if (txtTopLeft.Text != "") tl = Double.Parse(txtTopLeft.Text);
            Double tr = 0; if (txtTopRight.Text != "") tr = Double.Parse(txtTopRight.Text);
            Double br = 0; if (txtBottomRight.Text != "") br = double.Parse(txtBottomRight.Text);

            if (tl < bl || tr < tl || br < tr)
            {
                MessageBox.Show("Values of fuzzy set must be continous!");
                return false;
            }
            return true;

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}