using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using FRDB_SQLite.Class;

namespace FRDB_SQLite.Gui
{
    public partial class frmRelationEditor : DevExpress.XtraEditors.XtraForm
    {
        public frmRelationEditor()
        {
            InitializeComponent();
            GetListScheme();
            GetListRelation();
            FillJoinType();
            SchemeName = CreateRelation = OpenRelation = DeleteRelation =JoinRelation1 = JoinRelation2= JoinType = JoinName= String.Empty;
        }

        public String SchemeName { get; set; }
        public String CreateRelation { get; set; }
        public String OpenRelation { get; set; }
        public String DeleteRelation { get; set; }
        public String JoinType { get; set; }
        public String JoinRelation1 { get; set; }
        public String JoinRelation2 { get; set; }
        public String JoinName { get; set; }
        private void FillJoinType()
        {
            
        }
        private void GetListScheme()
        {
            if (DBValues.schemesName.Count != 0 && DBValues.schemesName != null)
            {
                foreach (var item in DBValues.schemesName)//DBValues.schemesName not null because we assigned before load in OpenScheme method
                {
                    cboSchemes.Items.Add(item);
                }

                cboSchemes.SelectedIndex = 0;
            }
        }

        private void GetListRelation()
        {
            if (DBValues.relationsName.Count != 0 && DBValues.relationsName != null)
            {
                foreach (var item in DBValues.relationsName)//DBValues.schemesName not null because we assigned before load in OpenScheme method
                {
                    cboRelations.Items.Add(item);
                    
                }

                cboRelations.SelectedIndex = 0;
             }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            
            if (cboSchemes.SelectedItem == null)
            {
                MessageBox.Show("Please select a scheme in order to reference!");
            }
            else
            {
                SchemeName = cboSchemes.SelectedItem.ToString();

                if (txtRelationName.Text == String.Empty)
                {
                    MessageBox.Show("Relation name empty!");
                }
                else if (DBValues.relationsName.Contains(txtRelationName.Text.ToString()))
                {
                    if (!Checker.NameChecking(txtRelationName.Text.Trim()))
                    {
                        MessageBox.Show("Your name can not contain special characters: " + Checker.GetSpecialCharaters());
                        return;
                    }
                    MessageBox.Show("This relation name has already existed in the database");
                }
                else
                {
                    CreateRelation = txtRelationName.Text;
                    this.Close();
                }
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (cboRelations.Items.Count <= 0)
            {
                MessageBox.Show("There are no relation for opening!");
            }
            else if (cboRelations.SelectedItem.ToString() == String.Empty)
            {
                MessageBox.Show("Please select an item from combobox!");
            }
            else
            {
                this.OpenRelation = cboRelations.SelectedItem.ToString();
                this.Close();
            }
        }

        private void txtDelete_Click(object sender, EventArgs e)
        {
            if (cboRelations.Items.Count <= 0)
            {
                MessageBox.Show("There are no relationgs for deleting!");
                return;
            }
            if (cboRelations.SelectedItem.ToString().Equals(""))
            {
                DeleteRelation = String.Empty;
            }
            else
            {
                DeleteRelation = cboRelations.SelectedItem.ToString();
            }

            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void labelControl7_Click(object sender, EventArgs e)
        {

        }

        private void frmRelationEditor_Load(object sender, EventArgs e)
        {

        }

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            
        }
    }
}