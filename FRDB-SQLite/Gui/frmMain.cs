using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using DevExpress.Skins;
using DevExpress.LookAndFeel;
using DevExpress.UserSkins;
using DevExpress.XtraEditors;
using DevExpress.XtraBars.Helpers;
using System.Timers;
using DevExpress.Utils.Menu;
using System.Threading;
using FRDB_SQLite;
using FRDB_SQLite;
using FRDB_SQLite;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using FRDB_SQLite.Class;
using DevExpress.XtraBars.Ribbon;
using System.IO;
using System.Diagnostics;



namespace FRDB_SQLite.Gui
{
    public partial class frmMain : XtraForm
    {
        public frmMain()
        {
            InitializeComponent();
            InitSkinGallery();
            ContextMenu_Database.Items[0].Visible = false;

        }

        #region 0. Declare

        ////////////////////////////////////////////////////////////////////////////
        ///fuzzy database object
        ////////////////////////////////////////////////////////////////////////////
        private FdbEntity fdbEntity;
        private FzSchemeEntity newScheme, currentScheme;
        private FzRelationEntity currentRelation, newRelation, renamedRelation;
        private FzQueryEntity currentQuery;
        private String path = "";
        private String _errorMessage;
        private Boolean _error;
        private List<FzRelationEntity> lrlAB = new List<FzRelationEntity>();

        private FzRelationEntity rlAB = new FzRelationEntity();
        private List<FzAttributeEntity> atAB = new List<FzAttributeEntity>();
        private List<FzAttributeEntity> _selectedAttributesAB = new List<FzAttributeEntity>();
        public String _conditionTextAB = String.Empty;
        private String[] _selectedAttributeTextsAB = null;
        private String[] _selectedRelationTextsAB = null;

        List<int> _indexAB = new List<int>();
        List<int> _indexRLAB = new List<int>();
        
        public String ErrorMessage
        {
            get { return _errorMessage; }
            set { this._errorMessage = value; }
        }
        public Boolean Error
        {
            get { return _error; }
            set { _error = value; }
        }

        ////////////////////////////////////////////////////////////////////////////
        ///BLL object
        ////////////////////////////////////////////////////////////////////////////
        private FdbBLL fdbBll;
        private ThreadBLL threadBLL;

        ////////////////////////////////////////////////////////////////////////////
        ///Tree List
        ////////////////////////////////////////////////////////////////////////////
        //TreeListNode parentFdbNode, childSchemeNode, childRelationNode, childQueryNode, childCurrentNode, childNewNode;
        private TreeNode parentFdbNode, childSchemeNode, childRelationNode, childQueryNode, childCurrentNode, childNewNode;
        public struct ImageTree
        {
            public int unselectedState;
            public int selectedState;
        }
        private ImageTree parentFdbImageTree, folderImageTree, schemeImageTree, relationImageTree, queryImageTree;

        ////////////////////////////////////////////////////////////////////////////
        ///other object and declare
        ////////////////////////////////////////////////////////////////////////////
        private System.Timers.Timer timer;
        private int currentRow, currentCell;
        private Boolean flag, validated, rollbackCell;

        #endregion

        #region 1. Home Ribbon Page

        private void CreateFuzzyDatabase()
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Create New Fuzzy Relational Database (FRDB)";
                sfd.Filter = "Database file (*.tbb) | *.tbb | All files (*.*) | *.* ";
                sfd.DefaultExt = "tbb";
                sfd.AddExtension = true;
                sfd.RestoreDirectory = true;
                sfd.InitialDirectory = FdbBLL.GetRootPath(AppDomain.CurrentDomain.BaseDirectory.ToString());

                sfd.SupportMultiDottedExtensions = true;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    //Todo code here
                    siStatus.Caption = "Creating new blank fuzzy database...";
                    fdbEntity = null;

                    NewFuzzyDatabaseEntity(sfd.FileName);

                    if (!fdbBll.CreateFuzzyDatabase(fdbEntity))
                    {
                        MessageBox.Show("Cannot create new blank fuzzy database!");
                    }
                    else
                    {
                        ShowTreeList();//Create successfully and show treeList
                        ShowTreeListNode();
                        ActiveDatabase(true);
                        iNewDatabase.Enabled = false;
                        iOpenExistingDatabase.Enabled = false;
                    }
                }
            }
            catch (Exception ex)
            {
                DialogResult result = MessageBox.Show("You haven't installed SQLite yet, do you want to install SQLite right now?", "SQLite"
                    + fdbEntity.FdbName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Process.Start("SQLite-1.0.66.0-setup.exe");
                }
            }
        }

        private void OpenFuzzyDatabase()
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.DefaultExt = "tbb";
                ofd.CheckFileExists = true;
                ofd.Filter = "Fuzzy Database File (*.tbb) | *.tbb";
                ofd.AddExtension = true;
                ofd.Multiselect = false;
                ofd.RestoreDirectory = true;
                ofd.Title = "Open Fuzzy Database...";

                String tmp = ReadPath();// Read the path
                if (tmp != "" && Directory.Exists(tmp))
                    ofd.InitialDirectory = tmp;
                else
                    ofd.InitialDirectory = FdbBLL.GetRootPath(AppDomain.CurrentDomain.BaseDirectory.ToString());

                ofd.SupportMultiDottedExtensions = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    siStatus.Caption = "Opening fuzzy database...";

                    NewFuzzyDatabaseEntity(ofd.FileName);
                    this.path = ofd.FileName;
                    WritePath(this.path.Substring(0, this.path.LastIndexOf("\\")));//Save the last path

                    threadBLL = new ThreadBLL(DBValues.connString);
                    threadBLL.WorkerThread = new Thread(new ThreadStart(threadBLL.StartWorker));
                    threadBLL.WorkerThread.Name = "Database Client Worker Thread";
                    threadBLL.WorkerThread.Start();
                    Cursor oldCursor = Cursor;
                    Cursor = Cursors.WaitCursor;
                    frmProgressBar frm = new frmProgressBar();
                    frm.Show();
                    frm.Refresh();
                    Boolean success = threadBLL.Connecting();
                    success = success && fdbBll.OpenFuzzyDatabase(fdbEntity);
                    frm.Close();
                    Cursor = oldCursor;

                    if (!success)
                    {
                        threadBLL.Dispose();
                        throw new Exception("ERROR:\n Can not connect to fuzzy database!");
                    }
                    else
                    {
                        ShowTreeList();
                        ShowTreeListNode();
                        ActiveDatabase(true);
                        iOpenExistingDatabase.Enabled = false;
                        iNewDatabase.Enabled = false;
                        threadBLL.Dispose();
                    }
                }

                ofd.Dispose();
            }
            catch (Exception ex)
            {
                DialogResult result = MessageBox.Show("You haven't installed SQLite yet, do you want to install SQLite right now?", "SQLite"
                   + fdbEntity.FdbName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    Process.Start("SQLite-1.0.66.0-setup.exe");
                }
            }
        }

        private void SaveFuzzyDatabase()
        {
            try
            {
                // Record to database
                Cursor oldCursor = Cursor;
                Cursor = Cursors.WaitCursor;
                frmProgressBar frm = new frmProgressBar();
                frm.LblName.Text = "Saving...";
                frm.Show();
                frm.Refresh();

                fdbBll = new FdbBLL();

                fdbBll.DropFuzzyDatabase(fdbEntity);
                if (!fdbBll.SaveFuzzyDatabase(fdbEntity))//Why fdbEntity doesn't null? Because it was created in  OpenFuzzyDatabase or CreateFuzzyDatabase
                {
                    siStatus.Caption = "Cannnot save the Database!";
                    timer.Start();
                }
                else
                {
                    siStatus.Caption = "The Database has been saved!";
                    timer.Start();
                }

                frm.Close();
                Cursor = oldCursor;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void SaveFuzzyDatabaseAs()
        {
            try
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.DefaultExt = "tbb";                                                                   // Default extension
                sfd.Filter = "Fuzzy Database File (*.tbb)|*.tbb|All files (*.*)|*.*";              // add extension to dialog
                sfd.AddExtension = true;                                                                // enable adding extension
                sfd.RestoreDirectory = true;                                                           // Automatic restore path for another time
                sfd.Title = "Save as...";
                sfd.InitialDirectory = FdbBLL.GetRootPath(AppDomain.CurrentDomain.BaseDirectory.ToString());
                sfd.SupportMultiDottedExtensions = true;

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    siStatus.Caption = "Saving Database...";

                    //NewFuzzyDatabaseEntity(sfd.FileName);
                    Clone(sfd.FileName);
                    this.path = sfd.FileName;

                    Cursor oldCursor = Cursor;
                    Cursor = Cursors.WaitCursor;

                    frmProgressBar frm = new frmProgressBar();
                    frm.LblName.Text = "Saving...";
                    frm.Show();
                    frm.Refresh();
                    //if (!fdbBll.SaveFuzzyDatabaseAs(fdbEntity))//Why doesn't fdbEntity null? Because it was created in  OpenFuzzyDatabase or CreateFuzzyDatabase
                    //{
                    //    siStatus.Caption = "Cannnot save the database!";
                    //    timer.Start();
                    //}
                    //else
                    //{
                    //    siStatus.Caption = "The database has been saved!";
                    //    timer.Start();
                    //}

                    frm.Close();
                    Cursor = oldCursor;
                    ShowTreeList();
                    ShowTreeListNode();
                    //Some enable control
                }

                sfd.Dispose();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CloseFuzzyDatabase()
        {
            try
            {
                DialogResult result = MessageBox.Show("Close current fuzzy database ?", "Close Fuzzy Database "
                    + fdbEntity.FdbName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    treeList1.Nodes.Clear();
                    fdbEntity = null;
                    CloseCurrentRelation();
                    AddRowDefault();
                    ResetObject();
                    ActiveDatabase(false);
                    iOpenExistingDatabase.Enabled = true;
                    iNewDatabase.Enabled = true;
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }
        }

        private void Clone(String path)
        {
            timer.Start();
            FdbBLL bll = new FdbBLL();
            FdbEntity newDatabase = new FdbEntity(path);
            bll.CreateFuzzyDatabase(newDatabase);
            newDatabase.Schemes = fdbEntity.Schemes;
            newDatabase.Relations = fdbEntity.Relations;
            newDatabase.Queries = fdbEntity.Queries;
            fdbEntity = null;
            DBValues.connString = newDatabase.ConnString;
            DBValues.dbName = newDatabase.FdbName;

            parentFdbNode.Text = DBValues.dbName.ToUpper();
            parentFdbNode.ToolTipText = "Database " + newDatabase.FdbName;
            fdbEntity = newDatabase;
        }

        private void NewFuzzyDatabaseEntity(String path)
        {
            timer.Start();

            fdbEntity = null;

            fdbBll = new FdbBLL();
            fdbEntity = new FdbEntity(path);
            this.path = path;

            DBValues.connString = fdbEntity.ConnString;
            DBValues.dbName = fdbEntity.FdbName;
        }

        private void iNewDatabase_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            CreateFuzzyDatabase();
        }

        private void iOpenExistingDatabase_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            OpenFuzzyDatabase();
        }

        private void iSave_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            SaveFuzzyDatabase();
        }

        private void iSaveAs_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            SaveFuzzyDatabaseAs();
        }

        private void iCloseDatabase_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            CloseFuzzyDatabase();
        }

        private void iRefreshDatabase_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (treeList1.Nodes.Count > 0)
            {
                ShowTreeList();
                ShowTreeListNode();
            }
        }

        private void iConnectDatabase_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (fdbEntity != null && FdbBLL.CheckConnection(fdbEntity))
            {
                MessageBox.Show("OK", "Connection is OK!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Connection is FAIL!");
            }
        }

        private void iExit_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            ClosingForm();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
                ClosingForm();
            else
            { }
        }

        private void ClosingForm()
        {
            try
            {
                if (fdbEntity != null)
                {
                    DialogResult result = MessageBox.Show("Do you want to save any change to database?", "Save changed", MessageBoxButtons.YesNoCancel);
                    if (result == DialogResult.Yes)
                    {
                        SaveFuzzyDatabase();
                        Application.Exit();
                    }
                    else if (result == DialogResult.No)
                        Application.Exit();
                    else
                    {
                        return;
                    }
                }
                else
                {
                    Application.Exit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can not save the database because of null values!", "ERROR", MessageBoxButtons.RetryCancel, MessageBoxIcon.Error);
            }

        }

        private void OnTimeEvent(Object sender, ElapsedEventArgs e)
        {
            siStatus.Caption = "Ready";
        }

        private String ReadPath()
        {
            String result = "";
            try
            {
                //FileStream fs = new FileStream("previous_path.txt", FileMode.Open);
                //StreamReader sr = new StreamReader(fs);
                //while (!sr.EndOfStream)
                //    result = sr.ReadLine();
                //fs.Close();
                using (StreamReader reader = new StreamReader("previous_path.txt"))
                {
                    result = reader.ReadLine();
                }
                return result;
            }
            catch (Exception ex)
            { return result; }
        }

        private void WritePath(String path)
        {
            try
            {
                //FileStream fs = new FileStream("previous_path.txt", FileMode.Create, FileAccess.Write);
                //StreamWriter sw = new StreamWriter(fs);
                //sw.WriteLine(path);
                //sw.Close();
                //fs.Close();
                using (StreamWriter writer = new StreamWriter("previous_path.txt"))
                {
                    writer.WriteLine(path);
                    writer.Close();
                }
            }
            catch (Exception ex)
            { }
        }

        #endregion
        #region Context Menu Fuzzy Database
        private void CTMenuDB_CloseDB_Click(object sender, EventArgs e)
        {
            CloseFuzzyDatabase();
        }

        private void CTMenuDB_Rename_Click(object sender, EventArgs e)
        {
            try
            {
                if (fdbEntity == null)
                {
                    MessageBox.Show("Current database is empty!");
                    return;
                }
                frmNewName frm = new frmNewName(1);
                frm.ShowDialog();
                if (frm.Name != null)
                {
                    //Save the database
                    DialogResult result = MessageBox.Show("Do you want to save all changed to the database ?", "Close Fuzzy Database "
                   + fdbEntity.FdbName, MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        new FdbBLL().DropFuzzyDatabase(fdbEntity);
                        new FdbBLL().SaveFuzzyDatabase(fdbEntity);
                    }

                    //Close the databse
                    treeList1.Nodes.Clear();
                    fdbEntity = null;
                    ResetObject();
                    ActiveDatabase(false);
                    iOpenExistingDatabase.Enabled = true;
                    iNewDatabase.Enabled = true;

                    //Change the name of the database
                    String path = "";
                    int length = this.path.LastIndexOf("\\");

                    path = this.path.Substring(0, length);
                    path += "\\" + frm.Name + ".tbb";

                    System.IO.File.Move(this.path, path);

                    //ReOpen the database
                    NewFuzzyDatabaseEntity(path);
                    if (new FdbBLL().OpenFuzzyDatabase(fdbEntity))
                    {
                        ShowTreeList();
                        ShowTreeListNode();
                        ActiveDatabase(true);
                        iOpenExistingDatabase.Enabled = false;
                        iNewDatabase.Enabled = false;
                    }

                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 2. Scheme Ribbon Page

        private void CreateNewBlankScheme()
        {
            try
            {
                if (fdbEntity == null)
                {
                    MessageBox.Show("ERROR:\n Haven't loaded Database yet!");
                    return;
                }

                DBValues.schemesName = FzSchemeBLL.GetListSchemeName(fdbEntity);
                frmSchemeEditor frm = new frmSchemeEditor();
                frm.ShowDialog();

                SchemeEditor(frm.CreateScheme, frm.OpenScheme, frm.DelecteScheme);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void OpenScheme()
        {
            try
            {
                if (fdbEntity == null)
                {
                    MessageBox.Show("ERROR:\n Haven't loaded Database yet!");
                    return;
                }

                DBValues.schemesName = FzSchemeBLL.GetListSchemeName(fdbEntity);
                frmSchemeEditor frm = new frmSchemeEditor();
                frm.ShowDialog();

                SchemeEditor(frm.CreateScheme, frm.OpenScheme, frm.DelecteScheme);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void SaveScheme()
        {
            try
            {
                if (!CheckSchemeData()) return;

                ///Current scheme is null that mean you dont open any schemes (the GridView is empty)
                ///Save new scheme OR save  attributes to an existed scheme
                ///Save new scheme: Save scheme name and list attributes
                ///Save to an existed scheme: Update list attributes (if it is not inherited)
                if (AllowSavingNewScheme())
                {
                    DBValues.schemesName = FzSchemeBLL.GetListSchemeName(fdbEntity);
                    frmSaveScheme frm = new frmSaveScheme();
                    frm.ShowDialog();

                    if (frm.SchemeName != String.Empty)
                    {
                        newScheme = FzSchemeBLL.GetSchemeByName(frm.SchemeName, fdbEntity);

                        if (newScheme == null)///Mean the scheme doesn't exist in the database. Saving scheme name
                        {
                            AddSchemeNode(frm.SchemeName);//Add scheme name to tree node and database
                        }

                        currentScheme = newScheme;
                        SaveCurrentScheme(frm.SchemeName);///Clear all attributes and update
                    }
                }
                ///This is exact "currentScheme"
                ///Save current scheme opened on GridView
                ///Check the inheritance (checked in SaveCurrentScheme())
                else
                {
                    String schemeName = currentScheme.SchemeName;
                    SaveCurrentScheme(schemeName);
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void DeleteScheme()
        {
            try
            {
                if (fdbEntity == null)
                {
                    MessageBox.Show("ERROR:\n Haven't loaded Database yet!");
                    return;
                }

                DBValues.schemesName = FzSchemeBLL.GetListSchemeName(fdbEntity);
                frmSchemeEditor frm = new frmSchemeEditor();
                frm.ShowDialog();

                SchemeEditor(frm.CreateScheme, frm.OpenScheme, frm.DelecteScheme);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void SaveCurrentScheme(String schemeName)///mean also save its attributes
        {
            if (FzSchemeBLL.IsInherited(currentScheme, fdbEntity.Relations))//Or check the readOnly on GridView
            {
                MessageBox.Show("Current Scheme is opened and \ninherited by some relations!");
                return;
            }

            xtraTabDatabase.TabPages[0].Text = "Scheme " + schemeName;
            xtraTabDatabase.SelectedTabPage = xtraTabDatabase.TabPages[0]; ;
            currentScheme.Attributes.Clear();
            GridViewDesign.CurrentCell = GridViewDesign.Rows[GridViewDesign.Rows.Count - 1].Cells[0];

            for (int i = 0; i < GridViewDesign.Rows.Count - 1; i++)// The end row is new row
            {
                Boolean primaryKey = Convert.ToBoolean(GridViewDesign.Rows[i].Cells[0].Value);
                String attributeName = GridViewDesign.Rows[i].Cells[1].Value.ToString();
                String typeName = GridViewDesign.Rows[i].Cells[2].Value.ToString();
                String description = (GridViewDesign.Rows[i].Cells[4].Value == null ? "" : GridViewDesign.Rows[i].Cells[4].Value.ToString());
                String domain = (GridViewDesign.Rows[i].Cells[3].Value.ToString());

                FzDataTypeEntity dataType = new FzDataTypeEntity(typeName, domain);
                FzAttributeEntity attribute = new FzAttributeEntity(primaryKey, attributeName, dataType, description);

                currentScheme.Attributes.Add(attribute);
            }

            if (GridViewDesign.Rows[GridViewDesign.Rows.Count - 2].Cells[1].Value.ToString() != "µ")
                AddMembership();

            MessageBox.Show("Current Scheme is saved OK!");
        }

        private void AddMembership()
        {
            MessageBox.Show("The default membership attribute \nwill be added automatically to this scheme", "", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Boolean primaryKey = false;
            String attributeName = "µ";
            String typeName = "Double";
            String description = "";
            String domain = "[5.0 x 10^-324  ...  1.7 x 10^308]";

            FzDataTypeEntity dataType = new FzDataTypeEntity(typeName, domain);
            FzAttributeEntity attribute = new FzAttributeEntity(primaryKey, attributeName, dataType, description);
            currentScheme.Attributes.Add(attribute);
        }

        private void SchemeEditor(String create, String open, String delete)
        {
            if (create != null)
            {
                AddSchemeNode(create);
                if (MessageBox.Show("Add attributes to this scheme?", "Add attributess", MessageBoxButtons.YesNo)
                    == DialogResult.Yes)
                {
                    currentScheme = FzSchemeBLL.GetSchemeByName(create, fdbEntity);
                    AddRowDefault();
                    xtraTabDatabase.TabPages[0].Text = "Scheme " + create;
                    xtraTabDatabase.SelectedTabPage = xtraTabDatabase.TabPages[0];
                }
            }
            if (open != null)
            {
                currentScheme = FzSchemeBLL.GetSchemeByName(open, fdbEntity);
                OpenScheme(currentScheme);
            }
            if (delete != null)
            {
                FzSchemeEntity delScheme = FzSchemeBLL.GetSchemeByName(delete, fdbEntity);
                DeleteScheme(delScheme);
            }
        }

        private void OpenScheme(FzSchemeEntity currentScheme)
        {
            //is not contented any attributes, need to add some attributes
            if (currentScheme.Attributes.Count == 0)
            {
                AddRowDefault();
                MessageBox.Show("There are no attribute in this scheme, let create some attributes!");
                xtraTabDatabase.TabPages[0].Text = "Create Attribute to Scheme " + currentScheme.SchemeName;
                xtraTabDatabase.SelectedTabPage = xtraTabDatabase.TabPages[0];

                UnsetReadOnlyGridView();
            }
            else//is contented, show text and attributes
            {
                xtraTabDatabase.TabPages[0].Text = "Scheme " + currentScheme.SchemeName;
                xtraTabDatabase.SelectedTabPage = xtraTabDatabase.TabPages[0];

                if (FzSchemeBLL.IsInherited(currentScheme, fdbEntity.Relations))
                {
                    SetReadOnlyGridView();//To prevent add attributes to current scheme
                }
                else UnsetReadOnlyGridView();

                ///Finally, show list attributes
                ShowAttribute();
            }
        }

        private void DeleteScheme(FzSchemeEntity delScheme)
        {
            if (FzSchemeBLL.IsInherited(delScheme, fdbEntity.Relations))
            {
                MessageBox.Show("Scheme is being inherited!");
            }
            else
            {
                DialogResult result = new DialogResult();
                result = MessageBox.Show("Delete this scheme ?", "Delete scheme " + delScheme.SchemeName, MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    if (currentScheme != null && delScheme.Equals(currentScheme))
                    {
                        //CloseCurrentScheme();
                        AddRowDefault();
                    }

                    DeleteTreeNode(delScheme.SchemeName, delScheme, null, null);
                }
            }
        }

        private void AddSchemeNode(String schemeName)
        {
            newScheme = new FzSchemeEntity(schemeName);
            fdbEntity.Schemes.Add(newScheme);

            TreeNode tempNode = new TreeNode();
            tempNode.Name = schemeName;
            tempNode.Text = schemeName;
            tempNode.ToolTipText = "Scheme " + schemeName;
            tempNode.ContextMenuStrip = ContextMenu_SchemaNode;
            tempNode.ImageIndex = schemeImageTree.selectedState;
            tempNode.SelectedImageIndex = schemeImageTree.unselectedState;
            childSchemeNode.Nodes.Add(tempNode);
        }

        private Boolean AllowSavingNewScheme()
        {
            if (currentScheme == null || xtraTabDatabase.TabPages[0].Text.Length <= 7)
                return true;
            return false;
        }

        private void SetReadOnlyGridView()
        {
            GridViewDesign.Columns[0].ReadOnly = true;
            GridViewDesign.Columns[1].ReadOnly = true;
            GridViewDesign.Columns[2].ReadOnly = true;
        }

        private void UnsetReadOnlyGridView()
        {
            GridViewDesign.Columns[0].ReadOnly = false;
            GridViewDesign.Columns[1].ReadOnly = false;
            GridViewDesign.Columns[2].ReadOnly = false;
        }

        private void ShowAttribute()
        {
            int n = GridViewDesign.Rows.Count - 2;
            for (int i = n; i >= 0; i--)
            {
                GridViewDesign.Rows.Remove(GridViewDesign.Rows[i]);
            }
            toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = "1 / 1";

            CheckBox chkbox;

            for (int i = 0; i < currentScheme.Attributes.Count - 1; i++)
            {
                FzAttributeEntity attr = currentScheme.Attributes[i];
                GridViewDesign.Rows.Add();
                chkbox = new CheckBox();
                chkbox.Checked = attr.PrimaryKey;
                GridViewDesign.Rows[i].Cells[0].Value = chkbox.CheckState;
                GridViewDesign.Rows[i].Cells[1].Value = attr.AttributeName;
                GridViewDesign.Rows[i].Cells[2].Value = attr.DataType.TypeName;
                GridViewDesign.Rows[i].Cells[3].Value = attr.DataType.DomainString;
                GridViewDesign.Rows[i].Cells[4].Value = (attr.Description != null ? attr.Description : null);
            }

            // GridViewDesign.CurrentCell = GridViewDesign.Rows[j].Cells[0];

            if (GridViewDesign.CurrentRow != null)
            {
                toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = (GridViewDesign.CurrentRow.Index + 1).ToString() + " / " + GridViewDesign.Rows.Count.ToString();
            }
            else toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = "1 / " + GridViewDesign.Rows.Count.ToString();
        }

        private bool CheckSchemeData()
        {
            if (fdbEntity == null)
            {
                MessageBox.Show("Please load database before you add any schemes!");
                return false;
            }

            if (currentScheme != null && FzSchemeBLL.IsInherited(currentScheme, fdbEntity.Relations))
            {
                MessageBox.Show("Current scheme is inherited, close this scheme and create new scheme!");
                return false;
            }

            ///GridViewDesign empty, nothing to save -> return
            if (GridViewDesign.Rows.Count <= 1)
            {
                MessageBox.Show("Please enter some attributes!");
                return false;
            }
            for (int i = 0; i < GridViewDesign.Rows.Count - 1; i++)
            {
                if (GridViewDesign.Rows[i].Cells[1].Value == null)
                {
                    MessageBox.Show("Attributes name is require at row[" + i + "]"); return false;
                }
                if (GridViewDesign.Rows[i].Cells[2].Value == null)
                {
                    MessageBox.Show("Datatype is require at row[" + i + "]"); return false;
                }
            }
            return true;
        }

        private void iNewScheme_ItemClick(object sender, EventArgs e)// DevExpress.XtraBars.ItemClickEventArgs e)
        {
            CreateNewBlankScheme();
        }

        private void iOpenScheme_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            OpenScheme();
        }

        private void iSaveScheme_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            SaveScheme();
        }

        private void iDeleteScheme_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            DeleteScheme();
        }

        private void iCloseCurrentScheme_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            currentScheme = null;
            AddRowDefault();
        }

        #endregion
        #region Context Menu Scheme
        private void CTMenuSchema_NewSchema_Click(object sender, EventArgs e)
        {
            CreateNewBlankScheme();
        }

        private void CTMenuSchNode_OpenSchema_Click(object sender, EventArgs e)
        {
            String schemeName = childCurrentNode.Name;
            treeList1.SelectedNode = childCurrentNode;
            currentScheme = FzSchemeBLL.GetSchemeByName(schemeName, fdbEntity);
            OpenScheme(currentScheme);
        }

        private void CTMenuSchNode_DeleteSchema_Click(object sender, EventArgs e)
        {
            try
            {
                String schemeName = childCurrentNode.Name;
                FzSchemeEntity deleteScheme = FzSchemeBLL.GetSchemeByName(schemeName, fdbEntity);

                DeleteScheme(deleteScheme);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void renameSchemeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                String oldName = childCurrentNode.Text;
                FzSchemeEntity newScheme = FzSchemeBLL.GetSchemeByName(oldName, fdbEntity);
                currentScheme = FzSchemeBLL.GetSchemeByName(oldName, fdbEntity);

                if (FzSchemeBLL.IsInherited(newScheme, fdbEntity.Relations))
                {
                    MessageBox.Show("This scheme is inherited by some relation!");
                    return;
                }
                DBValues.schemesName = FzSchemeBLL.GetListSchemeName(fdbEntity);
                frmNewName frm = new frmNewName(3);
                frm.ShowDialog();
                if (frm.Name == null) return;
                if (currentScheme != null && newScheme.Equals(currentScheme))
                {
                    if (xtraTabDatabase.TabPages[1].Text.Contains("Create Relation"))
                        xtraTabDatabase.TabPages[1].Text = "Create Relation " + frm.Name;
                    else
                        xtraTabDatabase.TabPages[1].Text = "Relation " + frm.Name;
                    childCurrentNode.Name = childCurrentNode.Text = frm.Name;
                }
                else
                    childCurrentNode.Name = childCurrentNode.Text = frm.Name;
                //Save to database
                FzSchemeBLL.RenameScheme(oldName, frm.Name, fdbEntity);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void deleteAllSchemesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (!FzSchemeBLL.IsInherited(fdbEntity))
                {
                    MessageBox.Show("Some scheme is being inherited\n Can not delete all schemes");
                    return;
                }
                DBValues.schemesName = FzSchemeBLL.GetListSchemeName(fdbEntity);

                foreach (var item in DBValues.schemesName)
                {
                    FzSchemeEntity tmp = new FzSchemeEntity();
                    tmp = FzSchemeBLL.GetSchemeByName(item, fdbEntity);
                    DeleteTreeNode(item, tmp, null, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        #endregion

        #region 3. Relation Ribbon Page

        private void CreateNewRelation()
        {
            try
            {
                if (fdbEntity == null)
                {
                    MessageBox.Show("Current database is empty! Please open!");
                    return;
                }

                if (fdbEntity.Schemes.Count == 0)
                {
                    MessageBox.Show("You must create scheme before creating any relations!");
                    return;
                }

                DBValues.schemesName = FzSchemeBLL.GetListSchemeName(fdbEntity);
                DBValues.relationsName = FzRelationBLL.GetListRelationName(fdbEntity);

                frmRelationEditor frm = new frmRelationEditor();
                frm.ShowDialog();

                RelationEditor(frm.CreateRelation, frm.OpenRelation, frm.DeleteRelation, frm.SchemeName, frm.JoinRelation1, frm.JoinRelation2, frm.JoinType, frm.JoinName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void OpenRelation()
        {
            try
            {
                if (fdbEntity == null)
                {
                    MessageBox.Show("Current database is empty! Please open!");
                    return;
                }

                DBValues.schemesName = FzSchemeBLL.GetListSchemeName(fdbEntity);
                DBValues.relationsName = FzRelationBLL.GetListRelationName(fdbEntity);

                frmRelationEditor frm = new frmRelationEditor();
                frm.ShowDialog();

                RelationEditor(frm.CreateRelation, frm.OpenRelation, frm.DeleteRelation, frm.SchemeName, frm.JoinRelation1, frm.JoinRelation2, frm.JoinType, frm.JoinName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void SaveRelation()
        {
            try
            {
                if (!CheckRelationData()) return;
                ///Current relation is assign when we create new relation, so it not null
                ///DBValues.schemesName = FzSchemeBLL.GetListSchemeName(fdbEntity);
                String RelationName = currentRelation.RelationName;
                xtraTabDatabase.TabPages[1].Text = "Relation " + RelationName;
                xtraTabDatabase.SelectedTabPage = xtraTabDatabase.TabPages[1]; ;

                SaveTuples(currentRelation);


                MessageBox.Show("Relation is saved OK!");
            }
            catch (Exception ex)
            {
                //MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void DeleteRelation()
        {
            try
            {
                if (fdbEntity == null)
                {
                    MessageBox.Show("Current database is empty! Please open!");
                    return;
                }

                DBValues.schemesName = FzSchemeBLL.GetListSchemeName(fdbEntity);
                DBValues.relationsName = FzRelationBLL.GetListRelationName(fdbEntity);
                frmRelationEditor frm = new frmRelationEditor();
                frm.ShowDialog();

                RelationEditor(frm.CreateRelation, frm.OpenRelation, frm.DeleteRelation, frm.SchemeName, frm.JoinRelation1, frm.JoinRelation2, frm.JoinType, frm.JoinName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void RelationEditor(String create, String open, String delete, String schemeName, string joinrelation1, string joinrelation2, string jointype, string joinname)
        {
            if (create != String.Empty)
            {
                AddRelationNode(create, schemeName);//Also add referenced scheme to Relation and add Relation to DB

            }
            if (open != String.Empty)
            {
                //IMPORT
                currentRelation = FzRelationBLL.GetRelationByName(open, fdbEntity);

                ///Show the columns attributes of current relation in order to add values
                ShowColumnsAttribute(currentRelation);

                ///Add tuples to relation
                ShowTuples(currentRelation);
            }
            if (joinrelation1 != String.Empty && joinrelation2 != String.Empty && jointype != String.Empty)
            {


            }
            if (delete != String.Empty)
            {
                FzRelationEntity deleteRelation = FzRelationBLL.GetRelationByName(delete, fdbEntity);

                DialogResult result = new DialogResult();
                result = MessageBox.Show("Delete this relation ?", "Delete relation " + delete, MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    if (currentRelation != null)
                    {
                        if (deleteRelation.Equals(currentRelation))
                        {
                            xtraTabDatabase.TabPages[1].Text = "Relation";
                            GridViewData.Rows.Clear();
                            GridViewData.Columns.Clear();
                            UpdateDataRowNumber();
                        }
                    }

                    ///Finally, remove NodeRelation and Relation in DB
                    TreeNode deletedNode = childRelationNode.Nodes[delete];
                    deletedNode.Remove();
                    fdbEntity.Relations.Remove(deleteRelation);
                    deleteRelation = null;

                    if (childRelationNode.Nodes.Count == 0)
                    {
                        childRelationNode.ImageIndex = childRelationNode.SelectedImageIndex = folderImageTree.unselectedState;
                    }
                }
            }
        }

        private void CloseCurrentRelation()
        {
            try
            {
                currentRelation = null;
                xtraTabDatabase.TabPages[1].Text = "Relation";
                GridViewData.Rows.Clear();
                GridViewData.Columns.Clear();
                UpdateDataRowNumber();
                //SwitchValueState(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void SaveTuples(FzRelationEntity currentRelation)
        {
            int nRow, nCol;
            nRow = GridViewData.Rows.Count - 1;
            nCol = GridViewData.Columns.Count;

            if (GridViewData.Rows.Count <= 1) return;
            GridViewData.CurrentCell = GridViewData.Rows[nRow].Cells[0];

            currentRelation.Tuples.Clear();

            for (int i = 0; i < nRow; i++)
            {
                List<Object> objs = new List<object>();

                for (int j = 0; j < nCol; j++)
                {
                    if (GridViewData.Rows[i].Cells[j].Value == null)
                    {
                        throw new Exception("Value cell is empty!");
                    }

                    objs.Add(GridViewData.Rows[i].Cells[j].Value);
                }

                FzTupleEntity tuple = new FzTupleEntity() 
{ ValuesOnPerRow = objs };
                currentRelation.Tuples.Add(tuple);
            }
        }

        private void ShowTuples(FzRelationEntity currentRelation)
        {
            if (currentRelation.Tuples.Count > 0)
            {
                int nRow = currentRelation.Tuples.Count;
                int nCol = currentRelation.Scheme.Attributes.Count;

                FzTupleEntity tuple;

                for (int i = 0; i < nRow; i++)      // Assign data for GridViewData
                {
                    tuple = currentRelation.Tuples[i];
                    GridViewData.Rows.Add();

                    for (int j = 0; j < nCol; j++)
                    {
                        GridViewData.Rows[i].Cells[j].Value = tuple.ValuesOnPerRow[j];
                    }

                }

                UpdateDataRowNumber();
            }
        }

        private void AddRelationNode(String relationName, String schemeName)
        {
            newRelation = new FzRelationEntity(relationName);
            newRelation.Scheme = FzSchemeBLL.GetSchemeByName(schemeName, fdbEntity);
            fdbEntity.Relations.Add(newRelation);
            TreeNode newNode = new TreeNode();
            newNode.Text = relationName;
            newNode.Name = relationName;
            newNode.ToolTipText = "Relation " + relationName;
            newNode.ContextMenuStrip = ContextMenu_RelationNode;
            newNode.ImageIndex = relationImageTree.unselectedState;
            newNode.SelectedImageIndex = relationImageTree.unselectedState;
            childRelationNode.Nodes.Add(newNode);

            currentRelation = newRelation;//Advoid null

            if (MessageBox.Show("Add values to this relation?", "Add values", MessageBoxButtons.YesNo)
                == DialogResult.Yes)
            {
                ShowColumnsAttribute(newRelation);
            }
        }
        private void AddJoin(String joinName, String relation1, String relation2)
        {

        }
        private void ShowColumnsAttribute(FzRelationEntity currentRelation)
        {

            xtraTabDatabase.TabPages[1].Text = "Relation " + currentRelation.RelationName;
            xtraTabDatabase.SelectedTabPage = xtraTabDatabase.TabPages[1];
            GridViewData.Rows.Clear();
            GridViewData.Columns.Clear();

            ///Add columns to relation
            int i = 0;
            foreach (FzAttributeEntity attr in currentRelation.Scheme.Attributes)
            {
                GridViewData.Columns.Add("Column " + i, attr.AttributeName);
                i++;
            }
        }

        private bool CheckRelationData()
        {
            if (fdbEntity == null)
            {
                MessageBox.Show("Current database is empty! Please open!");
                return false;
            }

            if (currentRelation == null)
            {
                MessageBox.Show("Current relation is null!"); return false;
            }
            for (int i = 0; i < GridViewData.Rows.Count - 1; i++)
            {
                for (int j = 0; j < GridViewData.Columns.Count; j++)
                {
                    if (GridViewData.Rows[i].Cells[j].Value == null)
                    {
                        MessageBox.Show("Value can not be empty!");
                        return false;
                    }
                }
            }
            return true;
        }

        private void iNewRelation_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            CreateNewRelation();
        }

        private void iOpenRelation_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            OpenRelation();
        }

        private void iSaveRelation_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            SaveRelation();
        }

        private void iDeleteRelation_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            DeleteRelation();
        }

        private void iCloseCurrentRelation_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            CloseCurrentRelation();
        }

        #endregion
        #region Context Menu Relation

        private void CTMenuRelation_NewRelation_Click(object sender, EventArgs e)
        {
            CreateNewRelation();
        }

        private void CTMenuRelNode_OpenRelation_Click(object sender, EventArgs e)
        {
            try
            {
                string relationName = childCurrentNode.Name;
                treeList1.SelectedNode = childCurrentNode;
                currentRelation = FzRelationBLL.GetRelationByName(relationName, fdbEntity);

                ShowColumnsAttribute(currentRelation);
                ShowTuples(currentRelation);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CTMenuRelation_DeleteRelations_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = new DialogResult();
                result = MessageBox.Show("Are you want to delete all relation ?", "Delete All Relations", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    childRelationNode.Nodes.Clear();
                    childCurrentNode = null;
                    xtraTabDatabase.TabPages[1].Text = "Relation";
                    GridViewData.Rows.Clear();
                    GridViewData.Columns.Clear();
                    UpdateDataRowNumber();
                    fdbEntity.Relations.Clear();
                    childRelationNode.ImageIndex = childRelationNode.SelectedImageIndex = 2;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CTMenuRelNode_DeleteRelation_Click(object sender, EventArgs e)
        {
            try
            {
                string relationName = childCurrentNode.Name;
                FzRelationEntity deleteRelation = FzRelationBLL.GetRelationByName(relationName, fdbEntity);

                DialogResult result = new DialogResult();
                result = MessageBox.Show("Are you  want to delete this relation ?", "Delete relation " + relationName, MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    if (deleteRelation.Equals(currentRelation))
                    {
                        xtraTabDatabase.TabPages[1].Text = "Relation";
                        GridViewData.Rows.Clear();
                        GridViewData.Columns.Clear();
                        UpdateDataRowNumber();
                    }

                    DeleteTreeNode(relationName, null, deleteRelation, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CTMenuRelNode_RenameRelation_Click(object sender, EventArgs e)
        {
            try
            {
                if (fdbEntity == null) { MessageBox.Show("Current Database is empty!"); return; }


                String relationName = "";
                if (currentRelation != null)
                    relationName = currentRelation.RelationName;
                else
                    relationName = childCurrentNode.Name;

                //Set currtn relation
                currentRelation = FzRelationBLL.GetRelationByName(relationName, fdbEntity);

                renamedRelation = FzRelationBLL.GetRelationByName(relationName, fdbEntity);

                DBValues.relationsName = FzRelationBLL.GetListRelationName(fdbEntity);
                frmNewName frm = new frmNewName(4);
                frm.ShowDialog();

                renamedRelation.RelationName = frm.Name;
                if (frm.Name == null) return;
                if (currentRelation != null)
                {
                    if (renamedRelation.Equals(currentRelation))
                    {
                        if (xtraTabDatabase.TabPages[1].Text.Contains("Create Relation"))
                            xtraTabDatabase.TabPages[1].Text = "Create Relation " + frm.Name;
                        else xtraTabDatabase.TabPages[1].Text = "Relation " + frm.Name;
                        childCurrentNode.Name = childCurrentNode.Text = frm.Name;
                    }
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region 4. Query Ribbon Page
        private void QueryEditor()
        {
            try
            {
                if (fdbEntity == null) { MessageBox.Show("Current database is empty!"); return; }

                frmQueryEditor frm = new frmQueryEditor(fdbEntity.Queries);
                frm.ShowDialog();
                //After form close
                fdbEntity.Queries = frm.Queries;
                DBValues.queriesName = FzQueryBLL.ListOfQueryName(fdbEntity);
                ShowTreeList();
                ShowTreeListNode();
                treeList1.ExpandAll();

                //Open query
                OpenQuery(frm.QueryName);

                if (currentQuery == null)
                    CloseCurrentQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void OpenQuery(String queryName)
        {
            if (queryName != null)
            {
                currentQuery = FzQueryBLL.GetQueryByName(queryName, fdbEntity);

                xtraTabDatabase.TabPages[2].Text = "Query " + queryName;
                xtraTabDatabase.SelectedTabPage = xtraTabDatabase.TabPages[2];
                txtQuery.Text = currentQuery.QueryString;
            }
        }

        private void SaveQuery()
        {
            try
            {
                if (txtQuery.Text != "")
                {
                    String queryName = xtraTabDatabase.TabPages[2].Text.Substring(5);
                    String queryText = txtQuery.Text;

                    frmQueryEditor frm = new frmQueryEditor(fdbEntity.Queries) { TxtQueryName = queryName, TxtQueryText = queryText };
                    frm.ShowDialog();

                    //After form close
                    fdbEntity.Queries = frm.Queries;
                    DBValues.queriesName = FzQueryBLL.ListOfQueryName(fdbEntity);
                    ShowTreeList();
                    ShowTreeListNode();
                    treeList1.ExpandAll();

                    currentQuery = null;
                    OpenQuery(frm.QueryName);
                }
                else
                {
                    xtraTabDatabase.SelectedTabPageIndex = 2;
                    txtQuery.Focus();
                    MessageBox.Show("Please input some text to save!");
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CloseCurrentQuery()
        {
            txtQuery.Text = "";
            xtraTabDatabase.SelectedTabPageIndex = 2;
            xtraTabDatabase.TabPages[2].Text = "Query";
        }

        private void iNewQuery_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            QueryEditor();
        }

        private void iOpenQuery_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            QueryEditor();
        }

        private void iSaveQuery_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            SaveQuery();
        }

        private void iDeleteQuery_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            currentQuery = null;
            QueryEditor();
            //CloseCurrentQuery();
        }

        private void iCloseCurrentQuery_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            CloseCurrentQuery();
        }

        #endregion
        #region Context Menu Query
        private void CTMenuQuery_NewQuery_Click(object sender, EventArgs e)
        {
            QueryEditor();
        }

        private void CTMenuQuery_DeleteQueries_Click(object sender, EventArgs e)
        {
            try
            {
                fdbEntity.Queries.Clear();
                ShowTreeList();
                ShowTreeListNode();
                treeList1.ExpandAll();
                currentQuery = null;
                CloseCurrentQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void CTMenuQueryNode_OpenQuery_Click(object sender, EventArgs e)
        {
            String queryName = childCurrentNode.Name;
            OpenQuery(queryName);
        }

        private void CTMenuQuery_DeleteQuery_Click(object sender, EventArgs e)
        {
            String queryName = childCurrentNode.Name;
            FzQueryEntity deleteQuery = FzQueryBLL.GetQueryByName(queryName, fdbEntity);
            MessageBox.Show(queryName);

            if (deleteQuery != null)
                DeleteTreeNode(queryName, null, null, deleteQuery);

        }

        private void CTMenuQuery_RenameQuery_Click(object sender, EventArgs e)
        {
            try
            {
                DBValues.queriesName = FzQueryBLL.ListOfQueryName(fdbEntity);
                frmNewName frm = new frmNewName(2);
                frm.ShowDialog();

                String oldName = childCurrentNode.Text;
                foreach (var item in fdbEntity.Queries)
                {
                    if (oldName.Equals(item.QueryName))
                    {
                        item.QueryName = frm.Name;
                        childCurrentNode.Text = frm.Name;
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void executeQueryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String queryName = childCurrentNode.Name;
            FzQueryEntity execute = FzQueryBLL.GetQueryByName(queryName, fdbEntity);

            if (execute != null)
            {
                xtraTabDatabase.SelectedTabPageIndex = 2;
                txtQuery.Text = execute.QueryString;
                ExecutingQuery();
            }
        }
        #endregion
        #region Query Processing
        private void frmMain_Load(object sender, EventArgs e)
        {

            QueryPL.txtQuery_TextChanged(txtQuery);
            AddRowDefault();
            StartApp();
        }

        private void ShowMessage(String message, Color color)
        {
            xtraTabDatabase.SelectedTabPageIndex = 2;
            xtraTabQueryResult.SelectedTabPageIndex = 1;
            // The type of error
            txtMessage.ForeColor = color;
            txtMessage.Text = message;
            return;
        }

        private void iOperator_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int pos = txtQuery.SelectionStart;
            if (txtQuery.Text == "") txtQuery.Text = "→";
            else txtQuery.Text = txtQuery.Text.Insert(pos, "→");
            txtQuery.SelectionStart = pos + 2;
        }
        //GetQueryTextsB
        private String GetQueryTextsB(String s)
        {//the relations which user input such: select attr1, att2... from
            String result = String.Empty;

            //String was standardzied and cut space,....
            int i = 0;
            if (s.Contains("natural join"))
            {
                i = s.IndexOf("natural join") + 13;

            }
            if (s.Contains("except"))
            {
                i = s.IndexOf("except") + 7;

            }
            if (s.Contains("union all"))
            {
                i = s.IndexOf("union all") + 10;

            }
            if (s.Contains("union") && (!s.Contains("union all")))
            {
                i = s.IndexOf("union") + 6;

            }

            if (s.Contains("intersect"))
            {
                i = s.IndexOf("intersect") + 10;

            }
            if (s.Contains("descartes"))
            {
                i = s.IndexOf("descartes") + 10;
            }
            result = s.Substring(i);
            return result;
        }
        private String GetRelationB(String s)
        {//the relations which user input such: select attr1, att2... from
            String tmp = String.Empty;

            //String was standardzied and cut space,....
            int i = 0;
            int j = 0;
            if (s.Contains("natural join") && s.Contains("where"))
            {
                i = s.IndexOf("natural join") + 13;
                j = s.IndexOf("where");
                tmp = s.Substring(i, j - i);
            }
            if (s.Contains("natural join") && (! s.Contains("where")))
            {
                i = s.IndexOf("natural join") + 13;
                tmp = s.Substring(i);
            }
            if (s.Contains("right join") && s.Contains("where"))
            {
                i = s.IndexOf("right join") + 11;
                j = s.IndexOf("where");
                tmp = s.Substring(i, j - i);

            }
            if (s.Contains("right join") && (!s.Contains("where")))
            {
                i = s.IndexOf("right join") + 11;
                tmp = s.Substring(i);
            }
            if (s.Contains("left join") && s.Contains("where"))
            {
                i = s.IndexOf("left join") + 10;
                j = s.IndexOf("where");
                tmp = s.Substring(i, j - i);

            }
            if (s.Contains("left join") && (!s.Contains("where")))
            {
                i = s.IndexOf("left join") + 10;
                tmp = s.Substring(i);
            }
            if (s.Contains("descartes") && s.Contains("where"))
            {
                i = s.IndexOf("descartes") + 10;
                j = s.IndexOf("where");
                tmp = s.Substring(i, j - i);

            }
            if (s.Contains("descartes") && (!s.Contains("where")))
            {
                i = s.IndexOf("descartes") + 10;
                tmp = s.Substring(i);
            }
            return tmp;
        }
        private String GetRelationTextsA(String s)
        {//the relations which user input such: select attr1, att2... from
            String tmp = String.Empty;

            //String was standardzied and cut space,....
            int i = s.IndexOf("from")+5;
            int j = 0;//query text doesn't contain any conditions
            if (s.Contains("natural join"))
            {
                j = s.IndexOf("natural join");
            }
            if (s.Contains("left join"))
            {
                j = s.IndexOf("left join");
            }
            if (s.Contains("right join"))
            {
                j = s.IndexOf("right join");
            }
            if (s.Contains("descartes"))
            {
                j = s.IndexOf("descartes");
            }
            if (s.Contains("union all"))
            {
                j = s.IndexOf("union all");
            }
            if (s.Contains("union") && (!s.Contains("union all")))
            {
                j = s.IndexOf("union");
            }

            if (s.Contains("intersect"))
            {
                j = s.IndexOf("intersect");
            }
            if (s.Contains("except"))
            {
                j = s.IndexOf("except");
            }

           tmp = s.Substring(i, j-i);
            return tmp;
        }

        private String GetQueryA(String s)
        {//the attributes which user input such: select attr1, att2... from
            String result = String.Empty;
            //String was standardzied and cut space,....
            int i = 0;
            if (s.Contains("natural join"))
            {
                i = s.IndexOf("natural join");
            }
            if (s.Contains("left join"))
            {
                i = s.IndexOf("left join");
            }
            if (s.Contains("right join"))
            {
                i = s.IndexOf("right join");
            }
            if (s.Contains("except"))
            {
                i = s.IndexOf("except");
            }
            if (s.Contains("union all"))
            {
                i = s.IndexOf("union all");
            }
            if (s.Contains("union") && (!s.Contains("union all")))
            {
                i = s.IndexOf("union");
            }
            if (s.Contains("intersect"))
            {
                i = s.IndexOf("intersect");
            }
            if (s.Contains("descartes"))
            {
                i = s.IndexOf("descartes");
            }
            result = s.Substring(0, i);
            return result;
        }
        public List<Item> FormatCondition(String condition)
        {
            List<Item> result = new List<Item>();
            int i = 0, j = 0, k = 0;

            while (i < condition.Length - 1)
            {
                if (condition[i] == '(')//(young=age) and (weight>=20 or height<=60)
                {
                    j = i + 1;
                    while (condition[j] != ')') j++;// Get index of ')'

                    Item item = new Item();
                    String exps = condition.Substring(i + 1, j - i - 1);
                    item.elements = SplitExpressions(exps);

                    // j < length, mean still expression in (...), get logicality
                    if (j != condition.Length - 1)
                    {
                        k = j + 1;
                        while (condition[k] != '(') k++;// Get index of next '('
                        item.nextLogic = condition.Substring(j + 1, k - j - 1);
                    }

                    result.Add(item);
                    if (j != condition.Length - 1) i = k - 1;
                    else i = j - 1;// end of the condition
                }
                i++;
            }

            return result;
        }
        private List<String> SplitExpressions(String exps)
        {
            List<String> result = new List<string>();
            int i = 0;
            if (exps.Length < 5)
            {
                AddToList(result, exps, exps.Length, "");
                return result;
            }
            while (i < exps.Length - 5)
            {
                if (!ContainLogicality(exps))
                {
                    // Add seperator; Split added and add to result; Also add the logicality
                    AddToList(result, exps, exps.Length, "");
                    //result.Add("");//For using in subQuery
                    return result;
                }

                String logic = exps.Substring(i, 5); // The logicality: " and ", " or ", " not "
                if (logic == " and ")
                {
                    // Add seperator; Split added and add to result; Also add the logicality
                    AddToList(result, exps, i, " and ");
                    exps = exps.Remove(0, i + 5);
                    i = -1;
                }
                else if (logic.Substring(0, logic.Length - 1) == " or ")
                {
                    // Add seperator; Split added and add to result; Also add the logicality
                    AddToList(result, exps, i, " or ");
                    exps = exps.Remove(0, i + 4);
                    i = -1;
                }
                else if (logic == " not ")
                {
                    // Add seperator; Split added and add to result; Also add the logicality
                    AddToList(result, exps, i, " not ");
                    exps = exps.Remove(0, i + 5);
                    i = -1;
                }
                i++;
            }
            return result;

        }
        private String AddSeperator(String expression)
        {
            //expression = expression.Replace(" ", "");//here
            for (int i = 1; i < expression.Length - 1; i++)
            {
                if (expression[i] == '<' || expression[i] == '>')
                {
                    expression = expression.Insert(i, "|"); i++;

                    if (expression[i + 1] == '=')
                    {
                        expression = expression.Insert(i + 2, "|"); i++;
                    }
                    else
                    {
                        expression = expression.Insert(i + 1, "|"); i++;
                    }
                }
                if (expression[i] == '=')
                {
                    if (expression[i - 1] == '!')
                    {
                        expression = expression.Insert(i - 1, "|"); i++;
                        expression = expression.Insert(i + 1, "|"); i++;
                    }
                    else if (expression[i - 1] != '<' && expression[i - 1] != '>')
                    {
                        expression = expression.Insert(i, "|"); i++;
                        expression = expression.Insert(i + 1, "|"); i++;
                    }
                }

                if (expression[i] == '→')
                {
                    expression = expression.Insert(i, "|");
                    expression = expression.Insert(i + 2, "|"); i += 2;
                }

            }
            return expression;
        }

        private void AddToList(List<String> result, String exps, int i, String logic)
        {
            String exp = exps.Substring(0, i);
            // Add seperator
            exp = AddSeperator(exp);
            //
            // Split added and add to result.
            List<String> splExps = Split(exp);
            foreach (string item in splExps)
            {
                result.Add(item.Trim());
            }
            // Also add the logicality
            if (logic != "") result.Add(logic);
        }
        private int IndexOfAttr(String s)
        {
            for (int i = 0; i < this.rlAB.Scheme.Attributes.Count; i++)
            {
                if (s.Equals(this.rlAB.Scheme.Attributes[i].AttributeName.ToLower()))
                {
                    return i;
                }
            }
            return -1;
        }
        private List<String> Split(String expression)
        {
            List<String> result = new List<string>();
            String[] splited = expression.Split('|');
            String[] tmp = new String[splited.Length];
            for (int i = 0; i < splited.Length; i++)
            {
                tmp[i] = splited[i].Trim();// Prevent user query with spaces
            }
            splited = tmp;
            ///Get index of attribulte and add to first of result
            ///after that add operator and value next to it
            int flag = -1;
            for (int i = 0; i < splited.Length; i++)
            {
                int index = IndexOfAttr(splited[i]);
                if (index >= 0)
                {
                    // Add the index of attribute
                    result.Add(index.ToString());
                    flag = i;
                    break;
                }
                else
                {
                    _errorMessage = "Invalid object name of attribute: '" + splited[i] + "'.";
                    _error = true;
                    throw new Exception(_errorMessage);
                }
            }

            // Add operator and value (maybe fuzzy value)
            if (flag == 0)//age=young
            {
                for (int i = 1; i < splited.Length - 1; i += 2)
                {
                    result.Add(splited[i]);//Add operator
                    result.Add(splited[i + 1]);//Add value (maybe fuzzy value)
                }
            }
            else if (flag == splited.Length - 1)//young=age
            {
                for (int i = splited.Length - 2; i > 0; i -= 2)
                {
                    result.Add(ReverseOperator(splited[i]));//Add operator
                    result.Add(splited[i - 1]);//Add value (maybe fuzzy value)
                }
            }
            else
                result = null;

            return result;

        }
        private String ReverseOperator(String op)
        {
            String result = op;
            if (op == "<")
                result = ">";
            if (op == "<=")
                result = ">=";
            if (op == ">")
                result = "<";
            if (op == ">=")
                result = "<=";

            return result;
        }

        private bool ContainLogicality(String exp)
        {
            if (exp.Contains(" and ") || exp.Contains(" or ") || exp.Contains(" not "))
                return true;
            return false;
        }

        private String GetConditionText(String s)
        {//the relations which user input such: select attr1, att2... from
            String result = String.Empty;
            //String was standardzied and cut space,....
            if (s.Contains("where"))
            {
                int i = s.IndexOf("where") + 6;// form where to the end of the string s (i is the first index to cut )
                result = s.Substring(i);
            }

            return result;
        }
        private String AddParenthesis(String condition)
        {
            for (int i = 0; i < condition.Length - 5; i++)
            {
                String logic = condition.Substring(i, 5);// " and ", " or ", " not ".
                if (condition[i] == '(')
                {
                    int j = i + 1;
                    while (condition[j] != ')') i = j++;
                    i -= 5;// Prevent Index was outside the bounds of the array
                }
                else
                {
                    if (logic == " and " || logic == " not ")
                    {
                        if (condition[i - 1] != ')' && condition[i + 5] != '(')
                        {
                            condition = condition.Insert(i + 5, "(");
                            condition = condition.Insert(i, ")");
                            i += 2;
                        }
                        else if (condition[i - 1] == ')' && condition[i + 5] != '(')
                        {
                            condition = condition.Insert(i + 5, "(");
                            i++;
                        }
                        else if (condition[i - 1] != ')' && condition[i + 5] == '(')
                            condition = condition.Insert(i++, ")");
                        i += 4;// Jump to the '('
                    }
                    if (logic.Substring(0, logic.Length - 1) == " or ")
                    {
                        if (condition[i - 1] != ')' && condition[i + 4] != '(')
                        {
                            condition = condition.Insert(i + 4, "(");
                            condition = condition.Insert(i, ")");
                            i += 2;
                        }
                        else if (condition[i - 1] == ')' && condition[i + 4] != '(')
                        {
                            condition = condition.Insert(i + 4, "(");
                            i++;
                        }
                        else if (condition[i - 1] != ')' && condition[i + 4] == '(')
                            condition = condition.Insert(i++, ")");
                        i += 3;// Jump to the '('
                    }
                }
            }
            if (condition[0] != '(')
                condition = condition.Insert(0, "(");
            if (condition[condition.Length - 1] != ')')
                condition += ")";

            return condition;
        }
        private void GetSectedRelation(List<FzRelationEntity> sets)
        {
            try
            {
                if (this._selectedRelationTextsAB != null)
                {
                    foreach (String text in this._selectedRelationTextsAB)
                    {
                        int i = 0;
                        foreach (FzRelationEntity item in sets)
                        {
                            if (text.Equals(item.RelationName.ToLower()))
                            {
                                this.lrlAB.Add(item);
                                _indexRLAB.Add(i);
                            }
                            i++;
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                this._error = true;
                this._errorMessage = ex.Message;
            }
        }
        private String[] GetRelationTexts(String s)
        {//the relations which user input such: select attr1, att2... from
            String[] result = null;

            //String was standardzied and cut space,....
            int i = s.IndexOf("from") + 5;
            int j = s.Length;//query text doesn't contain any conditions
            if (s.Contains("where"))//query text contains conditions
            {
                j = s.IndexOf("where");
            }
            else
            {
                if (s.Contains("oder by"))
                {
                    j = s.IndexOf("oder by");
                }
            }
            if (s.Contains("natural join"))
            {
                j = s.IndexOf("natural join");
            }
            if (s.Contains("left join"))
            {
                j = s.IndexOf("left join");
            }
            if (s.Contains("right join"))
            {
                j = s.IndexOf("right join");
            }
            if (s.Contains("descartes"))
            {
                j = s.IndexOf("descartes");
            }
            if (s.Contains("union all"))
            {
                j = s.IndexOf("union all");
            }
            if (s.Contains("union") && (!s.Contains("union all")))
            {
                j = s.IndexOf("union");
            }

            if (s.Contains("intersect"))
            {
                j = s.IndexOf("intersect");
            }
            if (s.Contains("except"))
            {
                j = s.IndexOf("except");
            }

            String tmp = s.Substring(i, j - i);
            tmp = tmp.Replace(" ", "");
            result = tmp.Split(',');

            return result;
        }


        private String[] GetAttributeTextsAB(String s)
        {//the attributes which user input such: select attr1, att2... from
            String[] result = null;
            if (s.Contains("min") || s.Contains("max") || s.Contains("sum") || s.Contains("avg"))
            {
                int left = s.IndexOf("(");
                int right = s.IndexOf(")");
                String condition = s.Substring(left + 1, right - left - 1);
                result = condition.Split(',');
            }
            //String was standardzied and cut space,....
            if (!s.Contains("*"))
            {
                int i = 7;//Attribute after "select"
                int j = s.IndexOf("from");
                String tmp = s.Substring(i, j - i);
                tmp = tmp.Replace(" ", "");
                result = tmp.Split(',');
            }
            return result;
        }

        private String GetQueryAB(String s)
        {//the attributes which user input such: select attr1, att2... from
            String result = String.Empty;
            //String was standardzied and cut space,....
            int i = 0;
            if (s.Contains("from"))
            {
                i = s.IndexOf("from");

            }
            
            result = s.Substring(0, i);

            return result;
        }
        private String ExistsAttribute()
        {
            String message = "";
            if (_selectedAttributeTextsAB == null) return "";

            foreach (var item in _selectedAttributeTextsAB)
            {
                int count = 0;
                String attr = item.ToLower();
                foreach (var item1 in this.rlAB.Scheme.Attributes)
                {
                    if (item1.AttributeName.ToLower() == attr)
                        count++;
                }

                if (count == 0)
                    return message = "Invalid selected object name of attribute: '" + attr + "'.";
            }
            return message;
        }
        private void GetSelectedAttr()
        {
            try
            {
                if (this._selectedAttributeTextsAB != null)
                {
                    foreach (String text in this._selectedAttributeTextsAB)
                    {
                        int i = 0;
                        foreach (FzAttributeEntity attr in this.rlAB.Scheme.Attributes)
                        {
                            if (text.Equals(attr.AttributeName.ToLower()))
                            {
                                this._selectedAttributesAB.Add(attr);
                                _indexAB.Add(i);
                            }
                            i++;
                        }
                    }
                    // Add the membership attribute
                    this._selectedAttributesAB.Add(this.rlAB.Scheme.Attributes[this.rlAB.Scheme.Attributes.Count - 1]);
                    //_index.Add(this._selectedRelations[0].Scheme.Attributes.Count - 1);// Add
                }
                else
                {
                    // Mean select * from ...
                    this._selectedAttributesAB = this.rlAB.Scheme.Attributes;
                }
            }
            catch (Exception ex)
            {
                this._error = true;
                this._errorMessage = ex.Message;
            }
        }
        private String ExistsFuzzySet(List<Item> items)
        {
            String message = "";
            FuzzyProcess fp = new FuzzyProcess();
            String path = Directory.GetCurrentDirectory() + @"\lib\";
            foreach (var item in items)
            {
                if (item.elements[1] == "->" || item.elements[1] == "→")
                {
                    if (!fp.Exists(path + item.elements[2] + ".conFS") &&
                        !fp.Exists(path + item.elements[2] + ".disFS"))
                    {
                        return message = "Incorrect fuzzy set: '" + item.elements[2] + "'.";
                    }
                }
            }

            return message;
        }
        private FzTupleEntity GetSelectedAttributes(FzTupleEntity resultTuple)
        {
            FzTupleEntity r = new FzTupleEntity();
            for (int i = 0; i < _indexAB.Count; i++)
            {
                for (int j = 0; j < resultTuple.ValuesOnPerRow.Count; j++)
                {
                    if (_indexAB[i] == j)
                    {
                        r.ValuesOnPerRow.Add(resultTuple.ValuesOnPerRow[j]);
                        break;
                    }
                }
            }
            r.ValuesOnPerRow.Add(resultTuple.ValuesOnPerRow[resultTuple.ValuesOnPerRow.Count - 1]);
            return r;
        }

        private void ExecutingQuery()
        {
            try
            {
                PrepareQuery();
                String query = QueryPL.StandardizeQuery(txtQuery.Text.Trim());
                String messagefirst = QueryPL.CheckSyntaxfirst(query);
                if (messagefirst != "")
                {
                    ShowMessage(messagefirst, Color.Red);
                    return;
                }
                FdbEntity newFdb = new FdbEntity() { Relations = fdbEntity.Relations, Schemes = fdbEntity.Schemes };

                if (query.Contains("descartes"))
                {
                    
                    _errorMessage = null;
                    _error = false;
                    rlAB = new FzRelationEntity();
                    atAB = new List<FzAttributeEntity>();
                    _selectedAttributesAB = new List<FzAttributeEntity>();
                    _conditionTextAB = String.Empty;
                    _selectedAttributeTextsAB = null;
                    _indexAB = new List<int>() ;
        
                    String relationA = GetRelationTextsA(query);
                    String queryA = "select * from " + relationA;
                    QueryPL.StandardizeQuery(queryA);
                    String message = QueryPL.CheckSyntax(queryA);
                    if (message != "")
                    {
                        ShowMessage(message, Color.Red);
                        return;
                    }
                    QueryExcutetionBLL excutetionA = null;
                    excutetionA = new QueryExcutetionBLL(queryA.ToLower(), newFdb.Relations);

                    String relationB = GetRelationB(query);
                    String queryB = "select * from " + relationB;
                    QueryPL.StandardizeQuery(queryB);
                    QueryExcutetionBLL excutetionB = new QueryExcutetionBLL(queryB.ToLower(), newFdb.Relations);
                    FzRelationEntity resultA = null;
                    resultA = excutetionA.ExecuteQuery();
                    if (excutetionA.Error)
                    {
                        ShowMessage(excutetionA.ErrorMessage, Color.Red); return;
                    }
                    FzRelationEntity resultB = null;
                    resultB = excutetionB.ExecuteQuery();
                    if (excutetionB.Error)
                    {
                        ShowMessage(excutetionB.ErrorMessage, Color.Red); return;
                    }
                    if (resultA != null && resultB != null)
                    {
                        if (query.Contains("descartes"))
                        {
                            int[] arrAB = new int[resultA.Scheme.Attributes.Count];
                            int same_att = 0;
                            int a = 0;
                            bool valueAB;
                            foreach (FzAttributeEntity att1 in resultA.Scheme.Attributes)
                            {
                                a++;
                                valueAB = false;
                                foreach (FzAttributeEntity att2 in resultB.Scheme.Attributes)
                                {
                                    if ((att1.AttributeName.Equals(att2.AttributeName)) && (a < resultA.Scheme.Attributes.Count))
                                    {
                                        valueAB = true;
                                        same_att++;

                                    }

                                }
                                if (valueAB == true)
                                {
                                    arrAB[a - 1] = 1;
                                }
                                else
                                {
                                    arrAB[a - 1] = 0;
                                }
                            }
                            if (same_att > 0)
                            {
                                ShowMessage("Between two relations, there mustn't be the same attribute", Color.Red);
                                return;
                            }
                            else
                            {
                                int ncol1 = resultA.Scheme.Attributes.Count;
                                int nrow1 = resultA.Tuples.Count;
                                int ncol2 = resultB.Scheme.Attributes.Count;
                                int nrow2 = resultB.Tuples.Count;
                                int m = 0;

                                FzTupleEntity tpl1;
                                FzTupleEntity tpl2;
                                foreach (FzAttributeEntity attribute2 in resultB.Scheme.Attributes)
                                {
                                    if (m == 0)
                                    {
                                        foreach (FzAttributeEntity attribute1 in resultA.Scheme.Attributes)
                                        {

                                            if (m < ncol1 - 1)
                                            {
                                                atAB.Add(attribute1);
                                                m++;

                                            }
                                        }
                                    }

                                    atAB.Add(attribute2);
                                    m++;


                                }

                                rlAB.Scheme.Attributes = atAB;
                                int r = 0;

                                for (int i = 0; i < nrow1; i++)
                                {
                                    tpl1 = resultA.Tuples[i];
                                    for (int j = 0; j < nrow2; j++)
                                    {
                                        List<Object> objs = new List<object>();

                                        tpl2 = resultB.Tuples[j];
                                        int k = 0;
                                        foreach (Object value1 in tpl1.ValuesOnPerRow)
                                        {

                                            if (k < ncol1 - 1)
                                            {
                                                objs.Add(value1.ToString());
                                                k++;

                                            }

                                        }
                                        foreach (Object value2 in tpl2.ValuesOnPerRow)
                                        {
                                            if ((k >= ncol1 - 1) && (k < m - 1))
                                            {
                                                objs.Add(value2.ToString());
                                                k++;
                                            }
                                            if (k == m - 1 && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) <= Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                            {
                                                objs.Add(tpl1.ValuesOnPerRow[ncol1 - 1]);
                                                k++;
                                            }
                                            if (k == m - 1 && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) > Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                            {
                                                objs.Add(tpl2.ValuesOnPerRow[ncol2 - 1]);
                                                k++;
                                            }
                                        }

                                        FzTupleEntity tplab = new FzTupleEntity() { ValuesOnPerRow = objs };
                                        rlAB.Tuples.Add(tplab);
                                        r++;

                                    }

                                }
                                String queryAB = GetQueryAB(query) + " from " + rlAB.RelationName;
                                QueryPL.StandardizeQuery(queryAB);
                                QueryExcutetionBLL excutetionAB = new QueryExcutetionBLL(queryAB.ToLower(), newFdb.Relations);
                                FzRelationEntity resultAB = new FzRelationEntity();
                                this._selectedAttributeTextsAB = GetAttributeTextsAB(query);
                                this._conditionTextAB = GetConditionText(query);
                                if (_conditionTextAB != String.Empty)
                                    this._conditionTextAB = AddParenthesis(this._conditionTextAB);
                                this.GetSelectedAttr();
                                if (this._error) throw new Exception(this._errorMessage);

                                _errorMessage = ExistsAttribute();
                                if (ErrorMessage != "") { this.Error = true; throw new Exception(_errorMessage); }
                                if (query.Contains("where"))
                                {
                                    List<Item> items = FormatCondition(this._conditionTextAB);
                                    //Check fuzzy set and object here
                                    this.ErrorMessage = ExistsFuzzySet(items);
                                    if (ErrorMessage != "")
                                    {

                                        ShowMessage(ErrorMessage, Color.Red); return;
                                    }

                                    QueryConditionBLL1 condition = new QueryConditionBLL1(items, this.rlAB);
                                    resultAB.Scheme.Attributes = this._selectedAttributesAB;

                                    foreach (FzTupleEntity tuple in this.rlAB.Tuples)
                                    {
                                        if (condition.Satisfy(items, tuple))
                                        {
                                            if (this._selectedAttributeTextsAB != null)
                                                resultAB.Tuples.Add(GetSelectedAttributes(condition.ResultTuple));
                                            else
                                                resultAB.Tuples.Add(condition.ResultTuple);
                                        }
                                    }
                                }
                                else// Select all tuples
                                {
                                    resultAB.Scheme.Attributes = this._selectedAttributesAB;
                                    resultAB.RelationName = this.rlAB.RelationName;

                                    if (this._selectedAttributeTextsAB != null)
                                    {
                                        foreach (var item in this.rlAB.Tuples)
                                            resultAB.Tuples.Add(GetSelectedAttributes(item));
                                    }
                                    else
                                    {
                                        foreach (var item in this.rlAB.Tuples)
                                            resultAB.Tuples.Add(item);
                                    }
                                }

                                if (resultAB != null)
                                {
                                    foreach (FzAttributeEntity attribute in resultAB.Scheme.Attributes)
                                        GridViewResult.Columns.Add(attribute.AttributeName, attribute.AttributeName);

                                    int j, i = -1;
                                    foreach (FzTupleEntity tuple in resultAB.Tuples)
                                    {
                                        GridViewResult.Rows.Add();
                                        i++; j = -1;
                                        foreach (Object value in tuple.ValuesOnPerRow)
                                            GridViewResult.Rows[i].Cells[++j].Value = value.ToString();
                                    }

                                    xtraTabQueryResult.SelectedTabPageIndex = 0;
                                    //txtMessage.Text = "There are "+ GridViewResult.RowCount+" row(s) affected";
                                }
                                else
                                {
                                    txtMessage.Text = "There is no relation satisfy the condition";
                                    xtraTabQueryResult.SelectedTabPageIndex = 1;

                                }
                            }
                            

                        }
                        else
                        {
                            txtMessage.Text = "There is no relation satisfy the condition";
                            xtraTabQueryResult.SelectedTabPageIndex = 1;
                            
                        }
                        siStatus.Caption = "Ready";
                        txtMessage.ForeColor = Color.Black;
                        txtMessage.Text = "There are " + GridViewResult.RowCount + " row(s) affected";

                    }

                }
                else
                {

                    if (query.Contains("natural join"))
                    {
                        _errorMessage = null;
                        _error = false;
                        rlAB = new FzRelationEntity();
                        atAB = new List<FzAttributeEntity>();
                        _selectedAttributesAB = new List<FzAttributeEntity>();
                        _conditionTextAB = String.Empty;
                        _selectedAttributeTextsAB = null;
                        _indexAB = new List<int>();
                        String relationA = GetRelationTextsA(query);
                        String queryA = "select * from " + relationA;
                        QueryPL.StandardizeQuery(queryA);
                        String message = QueryPL.CheckSyntax(queryA);
                        if (message != "")
                        {
                            ShowMessage(message, Color.Red);
                            return;
                        }
                        QueryExcutetionBLL excutetionA = null;
                        excutetionA = new QueryExcutetionBLL(queryA.ToLower(), newFdb.Relations);

                        String relationB = GetRelationB(query);
                        String queryB = "select * from " + relationB;
                        QueryPL.StandardizeQuery(queryB);
                        QueryExcutetionBLL excutetionB = new QueryExcutetionBLL(queryB.ToLower(), newFdb.Relations);
                        FzRelationEntity resultA = null;
                        resultA = excutetionA.ExecuteQuery();
                        if (excutetionA.Error)
                        {
                            ShowMessage(excutetionA.ErrorMessage, Color.Red); return;
                        }
                        FzRelationEntity resultB = null;
                        resultB = excutetionB.ExecuteQuery();
                        if (excutetionB.Error)
                        {
                            ShowMessage(excutetionB.ErrorMessage, Color.Red); return;
                        }

                        if (resultA != null && resultB != null)
                        {
                            if (query.Contains("natural join"))
                            {

                                int ncol1 = resultA.Scheme.Attributes.Count;
                                int nrow1 = resultA.Tuples.Count;
                                int ncol2 = resultB.Scheme.Attributes.Count;
                                int nrow2 = resultB.Tuples.Count;
                                int m = 0;
                                int[] arr1 = new int[ncol1];
                                bool same;
                                int stt = 0;
                                int stt2;
                                int dem = 0;
                                int same1 = 0;
                                int same2 = 0;
                                foreach (FzAttributeEntity attribute1 in resultA.Scheme.Attributes)
                                {

                                    same = false;
                                    stt++;
                                    stt2 = 0;
                                    foreach (FzAttributeEntity attribute2 in resultB.Scheme.Attributes)
                                    {
                                        stt2++;
                                        if ((attribute1.AttributeName.ToString() == attribute2.AttributeName.ToString()))
                                        {
                                            dem++;
                                            same = true;
                                            arr1[stt - 1] = 1;
                                            if (dem == 1)
                                            {
                                                same1 = stt;
                                                same2 = stt2;
                                            }
                                           
                                        }
                                       
                                    }
                                    if (same == false && (m < ncol1 - 2))
                                    {
                                        atAB.Add(attribute1);
                                        m++;
                                        arr1[stt - 1] = 0;
                                    }
                                    if (m == ncol1 - 2)
                                    {
                                        foreach (FzAttributeEntity attribute2 in resultB.Scheme.Attributes)
                                        {
                                            atAB.Add(attribute2);
                                            m++;
                                        }
                                    }
                                }
                                rlAB.Scheme.Attributes = atAB;
                                
                                int row = 0;
                                if (dem != 2)
                                {
                                    ShowMessage("Between two ralations, there is only one the same attribute", Color.Red);
                                    return;
                                }
                                else
                                {

                                    FzTupleEntity tpl1;
                                    FzTupleEntity tpl2;
                                    for (int i = 0; i < nrow1; i++)
                                    {
                                        tpl1 = resultA.Tuples[i];
                                        for (int j = 0; j < nrow2; j++)
                                        {
                                            tpl2 = resultB.Tuples[j];
                                            if (tpl1.ValuesOnPerRow[same1 - 1].ToString() == tpl2.ValuesOnPerRow[same2 - 1].ToString())
                                            {
                                                List<Object> objs = new List<object>();

                                                int k = 0;
                                                int stt_cell;
                                                stt_cell = 0;
                                                foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                {
                                                    stt_cell++;

                                                    if ((k < ncol1 - 2) && (arr1[stt_cell - 1] == 0))
                                                    {
                                                        objs.Add(value1.ToString());
                                                        k++;
                                                    }

                                                }
                                                foreach (Object value2 in tpl2.ValuesOnPerRow)
                                                {
                                                    if ((k >= (ncol1 - 2)) && (k < m - 1))
                                                    {
                                                        objs.Add(value2.ToString());
                                                        k++;
                                                    }

                                                }
                                                if ((k == m - 1) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) >= Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                {

                                                    objs.Add(tpl1.ValuesOnPerRow[ncol1 - 1]);
                                                }
                                                else
                                                {
                                                    if ((k == m - 1) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) < Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                    {
                                                        objs.Add(tpl2.ValuesOnPerRow[ncol2 - 1]);
                                                    }

                                                }
                                                FzTupleEntity tplab = new FzTupleEntity() { ValuesOnPerRow = objs };
                                                rlAB.Tuples.Add(tplab);
                                                row++;

                                            }
                                        }
                                    }

                                    String queryAB = GetQueryAB(query) + " from " + rlAB.RelationName;
                                    QueryPL.StandardizeQuery(queryAB);
                                    QueryExcutetionBLL excutetionAB = new QueryExcutetionBLL(queryAB.ToLower(), newFdb.Relations);
                                    FzRelationEntity resultAB = new FzRelationEntity();
                                    this._selectedAttributeTextsAB = GetAttributeTextsAB(query);
                                    this._conditionTextAB = GetConditionText(query);
                                    if (_conditionTextAB != String.Empty)
                                        this._conditionTextAB = AddParenthesis(this._conditionTextAB);
                                    this.GetSelectedAttr();
                                    if (this._error) throw new Exception(this._errorMessage);

                                    _errorMessage = ExistsAttribute();
                                    if (ErrorMessage != "") { this.Error = true; throw new Exception(_errorMessage); }
                                    if (query.Contains("where"))
                                    {
                                        List<Item> items = FormatCondition(this._conditionTextAB);
                                        //Check fuzzy set and object here
                                        this.ErrorMessage = ExistsFuzzySet(items);
                                        if (ErrorMessage != "")
                                        {

                                            ShowMessage(ErrorMessage, Color.Red); return;
                                        }

                                        QueryConditionBLL1 condition = new QueryConditionBLL1(items, this.rlAB);
                                        resultAB.Scheme.Attributes = this._selectedAttributesAB;

                                        foreach (FzTupleEntity tuple in this.rlAB.Tuples)
                                        {
                                            if (condition.Satisfy(items, tuple))
                                            {
                                                if (this._selectedAttributeTextsAB != null)
                                                    resultAB.Tuples.Add(GetSelectedAttributes(condition.ResultTuple));
                                                else
                                                    resultAB.Tuples.Add(condition.ResultTuple);
                                            }
                                        }
                                    }
                                    else// Select all tuples
                                    {
                                        resultAB.Scheme.Attributes = this._selectedAttributesAB;
                                        resultAB.RelationName = this.rlAB.RelationName;

                                        if (this._selectedAttributeTextsAB != null)
                                        {
                                            foreach (var item in this.rlAB.Tuples)
                                                resultAB.Tuples.Add(GetSelectedAttributes(item));
                                        }
                                        else
                                        {
                                            foreach (var item in this.rlAB.Tuples)
                                                resultAB.Tuples.Add(item);
                                        }
                                    }

                                    if (resultAB != null)
                                    {
                                        foreach (FzAttributeEntity attribute in resultAB.Scheme.Attributes)
                                            GridViewResult.Columns.Add(attribute.AttributeName, attribute.AttributeName);

                                        int j, i = -1;
                                        foreach (FzTupleEntity tuple in resultAB.Tuples)
                                        {
                                            GridViewResult.Rows.Add();
                                            i++; j = -1;
                                            foreach (Object value in tuple.ValuesOnPerRow)
                                                GridViewResult.Rows[i].Cells[++j].Value = value.ToString();
                                        }

                                        xtraTabQueryResult.SelectedTabPageIndex = 0;
                                        //txtMessage.Text = "There are " + GridViewResult.RowCount + " row(s) affected";
                                    }
                                    else
                                    {
                                        txtMessage.Text = "There is no relation satisfy the condition";
                                        xtraTabQueryResult.SelectedTabPageIndex = 1;

                                    }



                                }
                            }
                            else
                            {
                                txtMessage.Text = "There is no relation satisfy the condition";
                                xtraTabQueryResult.SelectedTabPageIndex = 1;
                                
                            }
                            siStatus.Caption = "Ready";
                            txtMessage.ForeColor = Color.Black;
                            txtMessage.Text = GridViewResult.RowCount + " row(s) affected";
                        }
                    }
                    //
                    else
                    {
                        if (query.Contains("union") && (!query.Contains("union all")))
                        {
                            String queryA = null;
                            queryA = GetQueryA(query);
                            QueryPL.StandardizeQuery(queryA);
                            String messageA = null;
                            messageA = QueryPL.CheckSyntax(queryA);
                            if (messageA != "")
                            {
                                ShowMessage(messageA, Color.Red);
                                return;
                            }
                            String relationB = GetQueryTextsB(query);
                            String queryB = relationB;
                            QueryPL.StandardizeQuery(queryB);
                            String messageB = null;
                            messageB = QueryPL.CheckSyntax(queryA);
                            if (messageB != "")
                            {
                                ShowMessage(messageB, Color.Red);
                                return;
                            }
                            QueryExcutetionBLL excutetionA = null;
                            excutetionA = new QueryExcutetionBLL(queryA.ToLower(), newFdb.Relations);
                            QueryExcutetionBLL excutetionB = null;
                            excutetionB = new QueryExcutetionBLL(queryB.ToLower(), newFdb.Relations);
                            FzRelationEntity resultA = null;
                            resultA = excutetionA.ExecuteQuery();
                            if (excutetionA.Error)
                            {
                                ShowMessage(excutetionA.ErrorMessage, Color.Red); return;
                            }
                            FzRelationEntity resultB = null;
                            resultB = excutetionB.ExecuteQuery();
                            if (excutetionB.Error)
                            {
                                ShowMessage(excutetionB.ErrorMessage, Color.Red); return;
                            }
                            if (resultA != null && resultB != null)
                            {
                                if (query.Contains("union") && (!query.Contains("union all")))
                                {
                                    int same_att = 0;
                                    int a = 0;
                                    foreach (FzAttributeEntity att1 in resultA.Scheme.Attributes)
                                    {
                                        a++;
                                        int b = 0;
                                        foreach (FzAttributeEntity att2 in resultB.Scheme.Attributes)
                                        {
                                            b++;
                                            if (att1.AttributeName.Equals(att2.AttributeName) && (a == b))
                                            {
                                                same_att++;
                                            }
                                        }
                                    }
                                    if (same_att != resultA.Scheme.Attributes.Count)
                                    {
                                        ShowMessage("The number of columns have to be same, data types have to be same and they have to be in same order.", Color.Red);
                                        return;
                                    }
                                    else
                                    {
                                        int ncol1 = resultA.Scheme.Attributes.Count;
                                        int nrow1 = resultA.Tuples.Count;
                                        int ncol2 = resultB.Scheme.Attributes.Count;
                                        int nrow2 = resultB.Tuples.Count;
                                        int m = 0;
                                      
                                        foreach (FzAttributeEntity attribute1 in resultA.Scheme.Attributes)
                                        {

                                            GridViewResult.Columns.Add(attribute1.AttributeName, attribute1.AttributeName);
                                            m++;
                                        }

                                        FzTupleEntity tpl1;
                                        FzTupleEntity tpl2;
                                        int r;
                                        r = -1;
                                        for (int i = 0; i < nrow1; i++)
                                        {
                                            tpl1 = resultA.Tuples[i];
                                           
                                           
                                            int stt = -1;
                                            Boolean valuecheck = false;
                                            for (int j = 0; j < nrow2; j++)
                                                {
                                                                                            
                                                   tpl2 = resultB.Tuples[j];
                                                   if (tpl1.ValuesOnPerRow[0].ToString() == tpl2.ValuesOnPerRow[0].ToString())
                                                   {
                                                       valuecheck = true;
                                                   }
                                                   if (r < nrow2-1)
                                                   {
                                                       r++;
                                                       int k2 = -1;
                                                       GridViewResult.Rows.Add();
                                                       foreach (Object value2 in tpl2.ValuesOnPerRow)
                                                       {
                                                           if(k2<m-2)
                                                           {
                                                               GridViewResult.Rows[j].Cells[++k2].Value = value2.ToString();
                                                           }
                                                           if(tpl1.ValuesOnPerRow[0].ToString() != tpl2.ValuesOnPerRow[0].ToString())
                                                           {
                                                               GridViewResult.Rows[j].Cells[m-1].Value = value2.ToString();
                                                           }
                                                       }
                                                       
                                                    }
                                                   if ((tpl1.ValuesOnPerRow[0].ToString() == tpl2.ValuesOnPerRow[0].ToString()) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) < Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                   {
                                                       GridViewResult.Rows[j].Cells[m - 1].Value = tpl1.ValuesOnPerRow[ncol1 - 1];

                                                   }
                                                   if ((tpl1.ValuesOnPerRow[0].ToString() == tpl2.ValuesOnPerRow[0].ToString()) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) >= Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                   {
                                                       GridViewResult.Rows[j].Cells[m - 1].Value = tpl2.ValuesOnPerRow[ncol2 - 1];

                                                   }
                                                
                                               
                                            }
                                            
                                            if (valuecheck == false)
                                            {
                                                 r++;
                                                int k = -1;
                                                GridViewResult.Rows.Add();
                                                foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                {
                                                   GridViewResult.Rows[r].Cells[++k].Value = value1.ToString();
                                                }
                                            }
                                        }
                                        xtraTabQueryResult.SelectedTabPageIndex = 0;
                                    }


                                }
                                else
                                {
                                    txtMessage.Text = "There is no relation satisfy the condition";
                                    xtraTabQueryResult.SelectedTabPageIndex = 1;
                                    
                                }
                                siStatus.Caption = "Ready";
                                txtMessage.ForeColor = Color.Black;
                                txtMessage.Text = GridViewResult.RowCount + " row(s) affected";
                            }
                        }
                        else
                        {
                            if (query.Contains("intersect"))
                            {
                                String queryA = null;
                                queryA = GetQueryA(query);
                                QueryPL.StandardizeQuery(queryA);
                                String messageA = null;
                                messageA = QueryPL.CheckSyntax(queryA);
                                if (messageA != "")
                                {
                                    ShowMessage(messageA, Color.Red);
                                    return;
                                }
                                String relationB = GetQueryTextsB(query);
                                String queryB = relationB;
                                QueryPL.StandardizeQuery(queryB);
                                String messageB = null;
                                messageB = QueryPL.CheckSyntax(queryA);
                                if (messageB != "")
                                {
                                    ShowMessage(messageB, Color.Red);
                                    return;
                                }
                                QueryExcutetionBLL excutetionA = null;
                                excutetionA = new QueryExcutetionBLL(queryA.ToLower(), newFdb.Relations);
                                QueryExcutetionBLL excutetionB = null;
                                excutetionB = new QueryExcutetionBLL(queryB.ToLower(), newFdb.Relations);
                                FzRelationEntity resultA = null;
                                resultA = excutetionA.ExecuteQuery();
                                if (excutetionA.Error)
                                {
                                    ShowMessage(excutetionA.ErrorMessage, Color.Red); return;
                                }
                                FzRelationEntity resultB = null;
                                resultB = excutetionB.ExecuteQuery();
                                if (excutetionB.Error)
                                {
                                    ShowMessage(excutetionB.ErrorMessage, Color.Red); return;
                                }
                                if (resultA != null && resultB != null)
                                {
                                    if (query.Contains("intersect"))
                                    {
                                        int same_att = 0;
                                        int a = 0;
                                        foreach (FzAttributeEntity att1 in resultA.Scheme.Attributes)
                                        {
                                            a++;
                                            int b = 0;
                                            foreach (FzAttributeEntity att2 in resultB.Scheme.Attributes)
                                            {
                                                b++;
                                                if (att1.AttributeName.Equals(att2.AttributeName) && (a == b))
                                                {
                                                    same_att++;
                                                }
                                            }
                                        }
                                        if (same_att != resultA.Scheme.Attributes.Count)
                                        {
                                            ShowMessage("The number of columns have to be same, data types have to be same and they have to be in same order.", Color.Red);
                                            return;
                                        }
                                        else
                                        {
                                            int ncol1 = resultA.Scheme.Attributes.Count;
                                            int nrow1 = resultA.Tuples.Count;
                                            int ncol2 = resultB.Scheme.Attributes.Count;
                                            int nrow2 = resultB.Tuples.Count;
                                            int m = 0;
                                            int row = -1;
                                            //option1
                                            foreach (FzAttributeEntity attribute1 in resultA.Scheme.Attributes)
                                            {
                                                GridViewResult.Columns.Add(attribute1.AttributeName, attribute1.AttributeName);
                                                m++;
                                            }


                                            FzTupleEntity tpl1;
                                            FzTupleEntity tpl2;

                                            for (int i = 0; i < nrow1; i++)
                                            {
                                                tpl1 = resultA.Tuples[i];
                                                    String valueCheck = null;
                                                    for (int j = 0; j < nrow2; j++)
                                                    {
                                                        tpl2 = resultB.Tuples[j];
                                                        foreach (Object value2 in tpl2.ValuesOnPerRow)
                                                        {
                                                            if ((tpl1.ValuesOnPerRow[0].ToString() == tpl2.ValuesOnPerRow[0].ToString()) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) > Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                            {
                                                                valueCheck = tpl2.ValuesOnPerRow[ncol2 - 1].ToString();
                                                            }
                                                            if ((tpl1.ValuesOnPerRow[0].ToString() == tpl2.ValuesOnPerRow[0].ToString()) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) <= Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                            {
                                                                valueCheck = tpl1.ValuesOnPerRow[ncol2 - 1].ToString();
                                                            }


                                                        }
                                                    }
                                                    if (valueCheck != null)
                                                    {
                                                        GridViewResult.Rows.Add();
                                                        int k = -1;
                                                        row++;
                                                        foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                        {

                                                            if (k < m - 2)
                                                            {
                                                                GridViewResult.Rows[i].Cells[++k].Value = value1.ToString();

                                                            }
                                                            if (k == m - 2)
                                                            {
                                                                GridViewResult.Rows[i].Cells[++k].Value = valueCheck;

                                                            }
                                                         }
                                                    }

                                               }
                                            xtraTabQueryResult.SelectedTabPageIndex = 0;
                                        }

                                    }
                                    else
                                    {
                                        txtMessage.Text = "There is no relation satisfy the condition";
                                        xtraTabQueryResult.SelectedTabPageIndex = 1;
                                        
                                    }
                                    siStatus.Caption = "Ready";
                                    txtMessage.ForeColor = Color.Black;
                                    txtMessage.Text = GridViewResult.RowCount + " row(s) affected";
                                }
                            }
                            else
                            {

                                if (query.Contains("except"))
                                {
                                    String queryA = null;
                                    queryA = GetQueryA(query);
                                    QueryPL.StandardizeQuery(queryA);
                                    String messageA = null;
                                    messageA = QueryPL.CheckSyntax(queryA);
                                    if (messageA != "")
                                    {
                                        ShowMessage(messageA, Color.Red);
                                        return;
                                    }
                                    String relationB = GetQueryTextsB(query);
                                    String queryB = relationB;
                                    QueryPL.StandardizeQuery(queryB);
                                    String messageB = null;
                                    messageB = QueryPL.CheckSyntax(queryA);
                                    if (messageB != "")
                                    {
                                        ShowMessage(messageB, Color.Red);
                                        return;
                                    }
                                    QueryExcutetionBLL excutetionA = null;
                                    excutetionA = new QueryExcutetionBLL(queryA.ToLower(), newFdb.Relations);
                                    QueryExcutetionBLL excutetionB = null;
                                    excutetionB = new QueryExcutetionBLL(queryB.ToLower(), newFdb.Relations);
                                    FzRelationEntity resultA = null;
                                    resultA = excutetionA.ExecuteQuery();
                                    if (excutetionA.Error)
                                    {
                                        ShowMessage(excutetionA.ErrorMessage, Color.Red); return;
                                    }
                                    FzRelationEntity resultB = null;
                                    resultB = excutetionB.ExecuteQuery();
                                    if (excutetionB.Error)
                                    {
                                        ShowMessage(excutetionB.ErrorMessage, Color.Red); return;
                                    }
                                    if (resultA != null && resultB != null)
                                    {
                                        if (query.Contains("except"))
                                        {
                                            int same_att = 0;
                                            int a = 0;
                                            foreach (FzAttributeEntity att1 in resultA.Scheme.Attributes)
                                            {
                                                a++;
                                                int b = 0;
                                                foreach (FzAttributeEntity att2 in resultB.Scheme.Attributes)
                                                {
                                                    b++;
                                                    if (att1.AttributeName.Equals(att2.AttributeName) && (a == b))
                                                    {
                                                        same_att++;
                                                    }
                                                }
                                            }
                                            if (same_att != resultA.Scheme.Attributes.Count)
                                            {
                                                ShowMessage("The number of columns have to be same, data types have to be same and they have to be in same order.", Color.Red);
                                                return;
                                            }
                                            else
                                            {
                                                int ncol1 = resultA.Scheme.Attributes.Count;
                                                int nrow1 = resultA.Tuples.Count;
                                                int ncol2 = resultB.Scheme.Attributes.Count;
                                                int nrow2 = resultB.Tuples.Count;
                                                int m = 0;
                                              
                                                foreach (FzAttributeEntity attribute1 in resultA.Scheme.Attributes)
                                                {

                                                    GridViewResult.Columns.Add(attribute1.AttributeName, attribute1.AttributeName);
                                                    m++;
                                                }


                                                FzTupleEntity tpl1;
                                                FzTupleEntity tpl2;

                                                for (int i = 0; i < nrow1; i++)
                                                {
                                                    tpl1 = resultA.Tuples[i];

                                                    GridViewResult.Rows.Add();
                                                    int k = -1;
                                                    foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                    {
                                                        String valueCheck = null;
                                                        for (int j = 0; j < nrow2; j++)
                                                        {
                                                            tpl2 = resultB.Tuples[j];
                                                            foreach (Object value2 in tpl2.ValuesOnPerRow)
                                                            {
                                                                if ((tpl1.ValuesOnPerRow[0].ToString() == tpl2.ValuesOnPerRow[0].ToString()) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) <= 1 - Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                                {
                                                                    valueCheck = tpl1.ValuesOnPerRow[ncol1 - 1].ToString();
                                                                }
                                                                if ((tpl1.ValuesOnPerRow[0].ToString() == tpl2.ValuesOnPerRow[0].ToString()) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) > 1 - Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                                {
                                                                    valueCheck = Convert.ToString(1 - Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1]));
                                                                }

                                                            }
                                                        }
                                                        if (valueCheck == null)
                                                        {
                                                            GridViewResult.Rows[i].Cells[++k].Value = value1.ToString();

                                                        }
                                                        if (valueCheck != null && k < m - 2)
                                                        {
                                                            GridViewResult.Rows[i].Cells[++k].Value = value1.ToString();

                                                        }
                                                        if (valueCheck != null && (k == m - 2))
                                                        {
                                                            GridViewResult.Rows[i].Cells[++k].Value = valueCheck;

                                                        }


                                                    }

                                                }
                                                xtraTabQueryResult.SelectedTabPageIndex = 0;
                                            }

                                        }
                                        else
                                        {
                                            txtMessage.Text = "There is no relation satisfy the condition";
                                            xtraTabQueryResult.SelectedTabPageIndex = 1;
                                            
                                        }
                                        siStatus.Caption = "Ready";
                                        txtMessage.ForeColor = Color.Black;
                                        txtMessage.Text = GridViewResult.RowCount + " row(s) affected";
                                    }
                                }
                                else
                                {
                                    if (query.Contains("union all"))
                                    {
                                        String queryA = null;
                                        queryA = GetQueryA(query);
                                        QueryPL.StandardizeQuery(queryA);
                                        String messageA = null;
                                        messageA = QueryPL.CheckSyntax(queryA);
                                        if (messageA != "")
                                        {
                                            ShowMessage(messageA, Color.Red);
                                            return;
                                        }
                                        String relationB = GetQueryTextsB(query);
                                        String queryB = relationB;
                                        QueryPL.StandardizeQuery(queryB);
                                        String messageB = null;
                                        messageB = QueryPL.CheckSyntax(queryA);
                                        if (messageB != "")
                                        {
                                            ShowMessage(messageB, Color.Red);
                                            return;
                                        }
                                        QueryExcutetionBLL excutetionA = null;
                                        excutetionA = new QueryExcutetionBLL(queryA.ToLower(), newFdb.Relations);
                                        QueryExcutetionBLL excutetionB = null;
                                        excutetionB = new QueryExcutetionBLL(queryB.ToLower(), newFdb.Relations);
                                        FzRelationEntity resultA = null;
                                        resultA = excutetionA.ExecuteQuery();
                                        if (excutetionA.Error)
                                        {
                                            ShowMessage(excutetionA.ErrorMessage, Color.Red); return;
                                        }
                                        FzRelationEntity resultB = null;
                                        resultB = excutetionB.ExecuteQuery();
                                        if (excutetionB.Error)
                                        {
                                            ShowMessage(excutetionB.ErrorMessage, Color.Red); return;
                                        }
                                        if (resultA != null && resultB != null)
                                        {
                                            if (query.Contains("union all"))
                                            {
                                                int same_att = 0;
                                                int a = 0;
                                                foreach (FzAttributeEntity att1 in resultA.Scheme.Attributes)
                                                {
                                                    a++;
                                                    int b = 0;
                                                    foreach (FzAttributeEntity att2 in resultB.Scheme.Attributes)
                                                    {
                                                        b++;
                                                        if (att1.AttributeName.Equals(att2.AttributeName) && (a == b))
                                                        {
                                                            same_att++;
                                                        }
                                                    }
                                                }
                                                if (same_att != resultA.Scheme.Attributes.Count)
                                                {
                                                    ShowMessage("The number of columns have to be same, data types have to be same and they have to be in same order.", Color.Red);
                                                    return;
                                                }
                                                else
                                                {
                                                    int ncol1 = resultA.Scheme.Attributes.Count;
                                                    int nrow1 = resultA.Tuples.Count;
                                                    int ncol2 = resultB.Scheme.Attributes.Count;
                                                    int nrow2 = resultB.Tuples.Count;
                                                    int m = 0;

                                                    foreach (FzAttributeEntity attribute1 in resultA.Scheme.Attributes)
                                                    {

                                                        GridViewResult.Columns.Add(attribute1.AttributeName, attribute1.AttributeName);
                                                        m++;
                                                    }

                                                    FzTupleEntity tpl1;
                                                    FzTupleEntity tpl2;
                                                    int r;
                                                    r = -1;
                                                    for (int i = 0; i < nrow1; i++)
                                                    {
                                                        tpl1 = resultA.Tuples[i];
                                                        for (int j = 0; j < nrow2; j++)
                                                        {
                                                            tpl2 = resultB.Tuples[j];
                                                            if (r < nrow2 - 1)
                                                            {
                                                                r++;
                                                                int k2 = -1;
                                                                GridViewResult.Rows.Add();
                                                                foreach (Object value2 in tpl2.ValuesOnPerRow)
                                                                {
                                                                        GridViewResult.Rows[j].Cells[++k2].Value = value2.ToString();
                                                                }
                                                            }
                                                          }
                                                            r++;
                                                            int k = -1;
                                                            GridViewResult.Rows.Add();
                                                            foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                            {
                                                                GridViewResult.Rows[r].Cells[++k].Value = value1.ToString();
                                                            }
                                                        }
                                                    xtraTabQueryResult.SelectedTabPageIndex = 0;
                                                }
                                              }
                                            else
                                            {
                                                txtMessage.Text = "There is no relation satisfy the condition";
                                                xtraTabQueryResult.SelectedTabPageIndex = 1;
                                                
                                            }
                                            siStatus.Caption = "Ready";
                                            txtMessage.ForeColor = Color.Black;
                                            txtMessage.Text = GridViewResult.RowCount + " row(s) affected";
                                        }
                                    }
                                    else
                                    {
                    if (query.Contains("left join"))
                    {
                        _errorMessage = null;
                        _error = false;
                        rlAB = new FzRelationEntity();
                        atAB = new List<FzAttributeEntity>();
                        _selectedAttributesAB = new List<FzAttributeEntity>();
                        _conditionTextAB = String.Empty;
                        _selectedAttributeTextsAB = null;
                        _indexAB = new List<int>();
                        String relationA = GetRelationTextsA(query);
                        String queryA = "select * from " + relationA;
                        QueryPL.StandardizeQuery(queryA);
                        String message = QueryPL.CheckSyntax(queryA);
                        if (message != "")
                        {
                            ShowMessage(message, Color.Red);
                            return;
                        }
                        QueryExcutetionBLL excutetionA = null;
                        excutetionA = new QueryExcutetionBLL(queryA.ToLower(), newFdb.Relations);

                        String relationB = GetRelationB(query);
                        String queryB = "select * from " + relationB;
                        QueryPL.StandardizeQuery(queryB);
                        QueryExcutetionBLL excutetionB = new QueryExcutetionBLL(queryB.ToLower(), newFdb.Relations);
                        FzRelationEntity resultA = null;
                        resultA = excutetionA.ExecuteQuery();
                        if (excutetionA.Error)
                        {
                            ShowMessage(excutetionA.ErrorMessage, Color.Red); return;
                        }
                        FzRelationEntity resultB = null;
                        resultB = excutetionB.ExecuteQuery();
                        if (excutetionB.Error)
                        {
                            ShowMessage(excutetionB.ErrorMessage, Color.Red); return;
                        }

                        if (resultA != null && resultB != null)
                        {
                            if (query.Contains("left join"))
                            {

                                int ncol1 = resultA.Scheme.Attributes.Count;
                                int nrow1 = resultA.Tuples.Count;
                                int ncol2 = resultB.Scheme.Attributes.Count;
                                int nrow2 = resultB.Tuples.Count;
                                int m = 0;
                                int[] arr1 = new int[ncol1];
                                bool same;
                                int stt = 0;
                                int stt2;
                                int dem = 0;
                                int same1 = 0;
                                int same2 = 0;
                                foreach (FzAttributeEntity attribute1 in resultA.Scheme.Attributes)
                                {

                                    same = false;
                                    stt++;
                                    stt2 = 0;
                                    foreach (FzAttributeEntity attribute2 in resultB.Scheme.Attributes)
                                    {
                                        stt2++;
                                        if ((attribute1.AttributeName.ToString() == attribute2.AttributeName.ToString()))
                                        {
                                            dem++;
                                            same = true;
                                            arr1[stt - 1] = 1;
                                            if (dem == 1)
                                            {
                                                same1 = stt;
                                                same2 = stt2;
                                            }
                                        }
                                        
                                    }
                                    if (same == false && (m < ncol1 - 2))
                                    {
                                        atAB.Add(attribute1);
                                        m++;
                                        arr1[stt - 1] = 0;
                                    }
                                    if (m == ncol1 - 2)
                                    {
                                        foreach (FzAttributeEntity attribute2 in resultB.Scheme.Attributes)
                                        {
                                            atAB.Add(attribute2);
                                            m++;
                                        }
                                    }
                                }
                                rlAB.Scheme.Attributes = atAB;
                                
                                int row = 0;
                                if (dem != 2)
                                {
                                    ShowMessage("Between two ralations, there is only one the same attribute", Color.Red);
                                    return;
                                }
                                else
                                {

                                    FzTupleEntity tpl1;
                                    FzTupleEntity tpl2;
                                    bool valuecheck ;
                                    for (int i = 0; i < nrow1; i++)
                                    {
                                        tpl1 = resultA.Tuples[i];
                                        valuecheck = false;
                                        for (int j = 0; j < nrow2; j++)
                                        {
                                            tpl2 = resultB.Tuples[j];
                                            if (tpl1.ValuesOnPerRow[same1 - 1].ToString() == tpl2.ValuesOnPerRow[same2 - 1].ToString())
                                            {
                                                valuecheck = true;
                                                List<Object> objs = new List<object>();
                                                int k = 0;
                                                int stt_cell;
                                                stt_cell = 0;
                                                foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                {
                                                    stt_cell++;

                                                    if ((k < ncol1 - 2) && (arr1[stt_cell - 1] == 0))
                                                    {
                                                        objs.Add(value1.ToString());
                                                        k++;
                                                    }

                                                }
                                                foreach (Object value2 in tpl2.ValuesOnPerRow)
                                                {
                                                    if ((k >= (ncol1 - 2)) && (k < m - 1))
                                                    {
                                                        objs.Add(value2.ToString());
                                                        k++;
                                                    }

                                                }
                                                if ((k == m - 1) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) >= Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                {

                                                    objs.Add(tpl1.ValuesOnPerRow[ncol1 - 1]);
                                                }
                                                else
                                                {
                                                    if ((k == m - 1) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) < Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                    {
                                                        objs.Add(tpl2.ValuesOnPerRow[ncol2 - 1]);
                                                    }

                                                }
                                                FzTupleEntity tplab = new FzTupleEntity() { ValuesOnPerRow = objs };
                                                rlAB.Tuples.Add(tplab);
                                                row++;

                                            }
                                        }
                                        if(valuecheck == false)
                                        {
                                                List<Object> objs = new List<object>();
                                                int k = 0;
                                                int stt_cell;
                                                stt_cell = 0;
                                                foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                {
                                                    stt_cell++;

                                                    if ((k < ncol1 - 2) && (arr1[stt_cell - 1] == 0))
                                                    {
                                                        objs.Add(value1.ToString());
                                                        k++;
                                                    }

                                                }
                                                for(int a = 0; a < ncol2;a++)
                                                {
                                                    if ((k >= (ncol1 - 2)) && (k < m - 1))
                                                    {
                                                        objs.Add("NULL");
                                                        k++;
                                                    }
                                                    if (k == m - 1)
                                                    {

                                                        objs.Add(tpl1.ValuesOnPerRow[ncol1 - 1]);
                                                        k++;
                                                    }
                                                

                                                }
                                                FzTupleEntity tplab = new FzTupleEntity() { ValuesOnPerRow = objs };
                                                rlAB.Tuples.Add(tplab);
                                                row++;
                                        }
                                    }

                                    String queryAB = GetQueryAB(query) + " from " + rlAB.RelationName;
                                    QueryPL.StandardizeQuery(queryAB);
                                    QueryExcutetionBLL excutetionAB = new QueryExcutetionBLL(queryAB.ToLower(), newFdb.Relations);
                                    FzRelationEntity resultAB = new FzRelationEntity();
                                    this._selectedAttributeTextsAB = GetAttributeTextsAB(query);
                                    this._conditionTextAB = GetConditionText(query);
                                    if (_conditionTextAB != String.Empty)
                                        this._conditionTextAB = AddParenthesis(this._conditionTextAB);
                                    this.GetSelectedAttr();
                                    if (this._error) throw new Exception(this._errorMessage);

                                    _errorMessage = ExistsAttribute();
                                    if (ErrorMessage != "") { this.Error = true; throw new Exception(_errorMessage); }
                                    if (query.Contains("where"))
                                    {
                                        List<Item> items = FormatCondition(this._conditionTextAB);
                                        //Check fuzzy set and object here
                                        this.ErrorMessage = ExistsFuzzySet(items);
                                        if (ErrorMessage != "")
                                        {

                                            ShowMessage(ErrorMessage, Color.Red); return;
                                        }

                                        QueryConditionBLL1 condition = new QueryConditionBLL1(items, this.rlAB);
                                        resultAB.Scheme.Attributes = this._selectedAttributesAB;

                                        foreach (FzTupleEntity tuple in this.rlAB.Tuples)
                                        {
                                            if (condition.Satisfy(items, tuple))
                                            {
                                                if (this._selectedAttributeTextsAB != null)
                                                    resultAB.Tuples.Add(GetSelectedAttributes(condition.ResultTuple));
                                                else
                                                    resultAB.Tuples.Add(condition.ResultTuple);
                                            }
                                        }
                                    }
                                    else// Select all tuples
                                    {
                                        resultAB.Scheme.Attributes = this._selectedAttributesAB;
                                        resultAB.RelationName = this.rlAB.RelationName;

                                        if (this._selectedAttributeTextsAB != null)
                                        {
                                            foreach (var item in this.rlAB.Tuples)
                                                resultAB.Tuples.Add(GetSelectedAttributes(item));
                                        }
                                        else
                                        {
                                            foreach (var item in this.rlAB.Tuples)
                                                resultAB.Tuples.Add(item);
                                        }
                                    }

                                    if (resultAB != null)
                                    {
                                        foreach (FzAttributeEntity attribute in resultAB.Scheme.Attributes)
                                            GridViewResult.Columns.Add(attribute.AttributeName, attribute.AttributeName);

                                        int j, i = -1;
                                        foreach (FzTupleEntity tuple in resultAB.Tuples)
                                        {
                                            GridViewResult.Rows.Add();
                                            i++; j = -1;
                                            foreach (Object value in tuple.ValuesOnPerRow)
                                                GridViewResult.Rows[i].Cells[++j].Value = value.ToString();
                                        }

                                        xtraTabQueryResult.SelectedTabPageIndex = 0;
                                        //txtMessage.Text = "There are " + GridViewResult.RowCount + " row(s) affected";
                                    }
                                    else
                                    {
                                        txtMessage.Text = "There is no relation satisfy the condition";
                                        xtraTabQueryResult.SelectedTabPageIndex = 1;

                                    }
                                }
                            }
                            else
                            {
                                txtMessage.Text = "There is no relation satisfy the condition";
                                xtraTabQueryResult.SelectedTabPageIndex = 1;
                                
                            }
                            siStatus.Caption = "Ready";
                            txtMessage.ForeColor = Color.Black;
                            txtMessage.Text = GridViewResult.RowCount + " row(s) affected";
                        }
                                        }
                                        else
                        if (query.Contains("right join"))
                        {
                            _errorMessage = null;
                            _error = false;
                            rlAB = new FzRelationEntity();
                            atAB = new List<FzAttributeEntity>();
                            _selectedAttributesAB = new List<FzAttributeEntity>();
                            _conditionTextAB = String.Empty;
                            _selectedAttributeTextsAB = null;
                            _indexAB = new List<int>();
                            String relationA = GetRelationTextsA(query);
                            String queryA = "select * from " + relationA;
                            QueryPL.StandardizeQuery(queryA);
                            String message = QueryPL.CheckSyntax(queryA);
                            if (message != "")
                            {
                                ShowMessage(message, Color.Red);
                                return;
                            }
                            QueryExcutetionBLL excutetionA = null;
                            excutetionA = new QueryExcutetionBLL(queryA.ToLower(), newFdb.Relations);

                            String relationB = GetRelationB(query);
                            String queryB = "select * from " + relationB;
                            QueryPL.StandardizeQuery(queryB);
                            QueryExcutetionBLL excutetionB = new QueryExcutetionBLL(queryB.ToLower(), newFdb.Relations);
                            FzRelationEntity resultA = null;
                            resultA = excutetionA.ExecuteQuery();
                            if (excutetionA.Error)
                            {
                                ShowMessage(excutetionA.ErrorMessage, Color.Red); return;
                            }
                            FzRelationEntity resultB = null;
                            resultB = excutetionB.ExecuteQuery();
                            if (excutetionB.Error)
                            {
                                ShowMessage(excutetionB.ErrorMessage, Color.Red); return;
                            }

                            if (resultA != null && resultB != null)
                            {
                                if (query.Contains("right join"))
                                {

                                    int ncol2 = resultA.Scheme.Attributes.Count;
                                    int nrow2 = resultA.Tuples.Count;
                                    int ncol1 = resultB.Scheme.Attributes.Count;
                                    int nrow1 = resultB.Tuples.Count;
                                    int m = 0;
                                    int[] arr1 = new int[ncol1];
                                    bool same;
                                    int stt = 0;
                                    int stt2;
                                    int dem = 0;
                                    int same1 = 0;
                                    int same2 = 0;
                                    foreach (FzAttributeEntity attribute1 in resultB.Scheme.Attributes)
                                    {

                                        same = false;
                                        stt++;
                                        stt2 = 0;
                                        foreach (FzAttributeEntity attribute2 in resultA.Scheme.Attributes)
                                        {
                                            stt2++;
                                            if ((attribute1.AttributeName.ToString() == attribute2.AttributeName.ToString()))
                                            {
                                                dem++;
                                                same = true;
                                                arr1[stt - 1] = 1;
                                                if (dem == 1)
                                                {
                                                    same1 = stt;
                                                    same2 = stt2;
                                                }
                                            }
                                           
                                        }
                                        if (same == false && (m < ncol1 - 2))
                                        {
                                            atAB.Add(attribute1);
                                            m++;
                                            arr1[stt - 1] = 0;
                                        }
                                        if (m == ncol1 - 2)
                                        {
                                            foreach (FzAttributeEntity attribute2 in resultA.Scheme.Attributes)
                                            {
                                                atAB.Add(attribute2);
                                                m++;
                                            }
                                        }
                                    }
                                    rlAB.Scheme.Attributes = atAB;

                                    int row = 0;
                                    if (dem != 2)
                                    {
                                        ShowMessage("Between two ralations, there is only one the same attribute", Color.Red);
                                        return;
                                    }
                                    else
                                    {

                                        FzTupleEntity tpl1;
                                        FzTupleEntity tpl2;
                                        bool valuecheck;
                                        for (int i = 0; i < nrow1; i++)
                                        {
                                            tpl1 = resultB.Tuples[i];
                                            valuecheck = false;
                                            for (int j = 0; j < nrow2; j++)
                                            {
                                                tpl2 = resultA.Tuples[j];
                                                if (tpl1.ValuesOnPerRow[same1 - 1].ToString() == tpl2.ValuesOnPerRow[same2 - 1].ToString())
                                                {
                                                    valuecheck = true;
                                                    List<Object> objs = new List<object>();
                                                    int k = 0;
                                                    int stt_cell;
                                                    stt_cell = 0;
                                                    foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                    {
                                                        stt_cell++;

                                                        if ((k < ncol1 - 2) && (arr1[stt_cell - 1] == 0))
                                                        {
                                                            objs.Add(value1.ToString());
                                                            k++;
                                                        }

                                                    }
                                                    foreach (Object value2 in tpl2.ValuesOnPerRow)
                                                    {
                                                        if ((k >= (ncol1 - 2)) && (k < m - 1))
                                                        {
                                                            objs.Add(value2.ToString());
                                                            k++;
                                                        }

                                                    }
                                                    if ((k == m - 1) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) >= Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                    {

                                                        objs.Add(tpl1.ValuesOnPerRow[ncol1 - 1]);
                                                    }
                                                    else
                                                    {
                                                        if ((k == m - 1) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) < Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                        {
                                                            objs.Add(tpl2.ValuesOnPerRow[ncol2 - 1]);
                                                        }

                                                    }
                                                    FzTupleEntity tplab = new FzTupleEntity() { ValuesOnPerRow = objs };
                                                    rlAB.Tuples.Add(tplab);
                                                    row++;

                                                }
                                            }
                                            if (valuecheck == false)
                                            {
                                                List<Object> objs = new List<object>();
                                                int k = 0;
                                                int stt_cell;
                                                stt_cell = 0;
                                                foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                {
                                                    stt_cell++;

                                                    if ((k < ncol1 - 2) && (arr1[stt_cell - 1] == 0))
                                                    {
                                                        objs.Add(value1.ToString());
                                                        k++;
                                                    }

                                                }
                                                for (int a = 0; a < ncol2; a++)
                                                {
                                                    if ((k >= (ncol1 - 2)) && (k < m - 1))
                                                    {
                                                        objs.Add("NULL");
                                                        k++;
                                                    }
                                                    if ((k == m - 1))
                                                    {
                                                        objs.Add(tpl1.ValuesOnPerRow[ncol1 - 1]);
                                                        k++;
                                                    }

                                                }
                                                
                                                FzTupleEntity tplab = new FzTupleEntity() { ValuesOnPerRow = objs };
                                                rlAB.Tuples.Add(tplab);
                                                row++;
                                            }
                                        }

                                        String queryAB = GetQueryAB(query) + " from " + rlAB.RelationName;
                                        QueryPL.StandardizeQuery(queryAB);
                                        QueryExcutetionBLL excutetionAB = new QueryExcutetionBLL(queryAB.ToLower(), newFdb.Relations);
                                        FzRelationEntity resultAB = new FzRelationEntity();
                                        this._selectedAttributeTextsAB = GetAttributeTextsAB(query);
                                        this._conditionTextAB = GetConditionText(query);
                                        if (_conditionTextAB != String.Empty)
                                            this._conditionTextAB = AddParenthesis(this._conditionTextAB);
                                        this.GetSelectedAttr();
                                        if (this._error) throw new Exception(this._errorMessage);

                                        _errorMessage = ExistsAttribute();
                                        if (ErrorMessage != "") { this.Error = true; throw new Exception(_errorMessage); }
                                        if (query.Contains("where"))
                                        {
                                            List<Item> items = FormatCondition(this._conditionTextAB);
                                            //Check fuzzy set and object here
                                            this.ErrorMessage = ExistsFuzzySet(items);
                                            if (ErrorMessage != "")
                                            {

                                                ShowMessage(ErrorMessage, Color.Red); return;
                                            }

                                            QueryConditionBLL1 condition = new QueryConditionBLL1(items, this.rlAB);
                                            resultAB.Scheme.Attributes = this._selectedAttributesAB;

                                            foreach (FzTupleEntity tuple in this.rlAB.Tuples)
                                            {
                                                if (condition.Satisfy(items, tuple))
                                                {
                                                    if (this._selectedAttributeTextsAB != null)
                                                        resultAB.Tuples.Add(GetSelectedAttributes(condition.ResultTuple));
                                                    else
                                                        resultAB.Tuples.Add(condition.ResultTuple);
                                                }
                                            }
                                        }
                                        else// Select all tuples
                                        {
                                            resultAB.Scheme.Attributes = this._selectedAttributesAB;
                                            resultAB.RelationName = this.rlAB.RelationName;

                                            if (this._selectedAttributeTextsAB != null)
                                            {
                                                foreach (var item in this.rlAB.Tuples)
                                                    resultAB.Tuples.Add(GetSelectedAttributes(item));
                                            }
                                            else
                                            {
                                                foreach (var item in this.rlAB.Tuples)
                                                    resultAB.Tuples.Add(item);
                                            }
                                        }

                                        if (resultAB != null)
                                        {
                                            foreach (FzAttributeEntity attribute in resultAB.Scheme.Attributes)
                                                GridViewResult.Columns.Add(attribute.AttributeName, attribute.AttributeName);

                                            int j, i = -1;
                                            foreach (FzTupleEntity tuple in resultAB.Tuples)
                                            {
                                                GridViewResult.Rows.Add();
                                                i++; j = -1;
                                                foreach (Object value in tuple.ValuesOnPerRow)
                                                    GridViewResult.Rows[i].Cells[++j].Value = value.ToString();
                                            }

                                            xtraTabQueryResult.SelectedTabPageIndex = 0;
                                            //txtMessage.Text = "There are " + GridViewResult.RowCount + " row(s) affected";
                                        }
                                        else
                                        {
                                            txtMessage.Text = "There is no relation satisfy the condition";
                                            xtraTabQueryResult.SelectedTabPageIndex = 1;

                                        }



                                    }
                                }
                                else
                                {
                                    txtMessage.Text = "There is no relation satisfy the condition";
                                    xtraTabQueryResult.SelectedTabPageIndex = 1;

                                }
                                siStatus.Caption = "Ready";
                                txtMessage.ForeColor = Color.Black;
                                txtMessage.Text = GridViewResult.RowCount + " row(s) affected";
                            }
                                            }
                                            else
                                            {
                                                this._selectedRelationTextsAB = null;
                                        lrlAB = new List<FzRelationEntity>();
                                        this._selectedRelationTextsAB = GetRelationTexts(query);
                                        this.GetSectedRelation(newFdb.Relations); 
                                        if (this._error) throw new Exception(this._errorMessage);
                                       
                                        int t= 0;
                                        t = lrlAB.Count;
                                        if (t >= 3)
                                        {
                                            ShowMessage("Don't support with three or more relations", Color.Red);
                                            return;
                                        }
                                        else
                                        {
                                            if (t == 2)
                                            {

                                                _errorMessage = null;
                                                _error = false;
                                                rlAB = new FzRelationEntity();
                                                atAB = new List<FzAttributeEntity>();
                                                _selectedAttributesAB = new List<FzAttributeEntity>();
                                                _conditionTextAB = String.Empty;
                                                _selectedAttributeTextsAB = null;
                                                _indexAB = new List<int>();

                                                String queryA = null;
                                                queryA = "select * from " + lrlAB[0].RelationName;
                                                QueryPL.StandardizeQuery(queryA);
                                                String messageA = null;
                                                messageA = QueryPL.CheckSyntax(queryA);
                                                if (messageA != "")
                                                {
                                                    ShowMessage(messageA, Color.Red);
                                                    return;
                                                }

                                                String queryB = "select * from " + lrlAB[1].RelationName;
                                                QueryPL.StandardizeQuery(queryB);
                                                String messageB = null;
                                                messageB = QueryPL.CheckSyntax(queryA);
                                                if (messageB != "")
                                                {
                                                    ShowMessage(messageB, Color.Red);
                                                    return;
                                                }
                                                QueryExcutetionBLL excutetionA = null;
                                                excutetionA = new QueryExcutetionBLL(queryA.ToLower(), newFdb.Relations);
                                                QueryExcutetionBLL excutetionB = null;
                                                excutetionB = new QueryExcutetionBLL(queryB.ToLower(), newFdb.Relations);
                                                FzRelationEntity resultA = null;
                                                resultA = excutetionA.ExecuteQuery();
                                                if (excutetionA.Error)
                                                {
                                                    ShowMessage(excutetionA.ErrorMessage, Color.Red); return;
                                                }
                                                FzRelationEntity resultB = null;
                                                resultB = excutetionB.ExecuteQuery();
                                                if (excutetionB.Error)
                                                {
                                                    ShowMessage(excutetionB.ErrorMessage, Color.Red); return;
                                                }
                                                if (resultA != null && resultB != null)
                                                {
                                                    int[] arrAB = new int[resultA.Scheme.Attributes.Count];
                                                    int same_att = 0;
                                                    int a = 0;
                                                    bool valueAB;
                                                    foreach (FzAttributeEntity att1 in resultA.Scheme.Attributes)
                                                    {
                                                        a++;
                                                        valueAB = false;
                                                        foreach (FzAttributeEntity att2 in resultB.Scheme.Attributes)
                                                        {
                                                            if ((att1.AttributeName.Equals(att2.AttributeName)) && (a < resultA.Scheme.Attributes.Count))
                                                            {
                                                                valueAB = true;
                                                                same_att++;

                                                            }

                                                        }
                                                        if (valueAB == true)
                                                        {
                                                            arrAB[a - 1] = 1;
                                                        }
                                                        else
                                                        {
                                                            arrAB[a - 1] = 0;
                                                        }
                                                    }
                                                    if (same_att == 0)
                                                    {
                                                        int ncol1 = resultA.Scheme.Attributes.Count;
                                                        int nrow1 = resultA.Tuples.Count;
                                                        int ncol2 = resultB.Scheme.Attributes.Count;
                                                        int nrow2 = resultB.Tuples.Count;
                                                        int m = 0;

                                                        FzTupleEntity tpl1;
                                                        FzTupleEntity tpl2;
                                                        foreach (FzAttributeEntity attribute2 in resultB.Scheme.Attributes)
                                                        {
                                                            if (m == 0)
                                                            {
                                                                foreach (FzAttributeEntity attribute1 in resultA.Scheme.Attributes)
                                                                {

                                                                    if (m < ncol1 - 1)
                                                                    {
                                                                        atAB.Add(attribute1);
                                                                        m++;

                                                                    }
                                                                }
                                                            }

                                                            atAB.Add(attribute2);
                                                            m++;


                                                        }

                                                        rlAB.Scheme.Attributes = atAB;
                                                        int r = 0;

                                                        for (int i = 0; i < nrow1; i++)
                                                        {
                                                            tpl1 = resultA.Tuples[i];
                                                            for (int j = 0; j < nrow2; j++)
                                                            {
                                                                List<Object> objs = new List<object>();

                                                                tpl2 = resultB.Tuples[j];
                                                                int k = 0;
                                                                foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                                {

                                                                    if (k < ncol1 - 1)
                                                                    {
                                                                        objs.Add(value1.ToString());
                                                                        k++;

                                                                    }

                                                                }
                                                                foreach (Object value2 in tpl2.ValuesOnPerRow)
                                                                {
                                                                    if ((k >= ncol1 - 1) && (k < m - 1))
                                                                    {
                                                                        objs.Add(value2.ToString());
                                                                        k++;
                                                                    }
                                                                    if (k == m - 1 && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) <= Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                                    {
                                                                        objs.Add(tpl1.ValuesOnPerRow[ncol1 - 1]);
                                                                        k++;
                                                                    }
                                                                    if (k == m - 1 && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) > Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                                    {
                                                                        objs.Add(tpl2.ValuesOnPerRow[ncol2 - 1]);
                                                                        k++;
                                                                    }
                                                                }

                                                                FzTupleEntity tplab = new FzTupleEntity() { ValuesOnPerRow = objs };
                                                                rlAB.Tuples.Add(tplab);
                                                                r++;

                                                            }

                                                        }
                                                        String queryAB = GetQueryAB(query) + " from " + rlAB.RelationName;
                                                        QueryPL.StandardizeQuery(queryAB);
                                                        QueryExcutetionBLL excutetionAB = new QueryExcutetionBLL(queryAB.ToLower(), newFdb.Relations);
                                                        FzRelationEntity resultAB = new FzRelationEntity();
                                                        this._selectedAttributeTextsAB = GetAttributeTextsAB(query);
                                                        this._conditionTextAB = GetConditionText(query);
                                                        if (_conditionTextAB != String.Empty)
                                                            this._conditionTextAB = AddParenthesis(this._conditionTextAB);
                                                        this.GetSelectedAttr();
                                                        if (this._error) throw new Exception(this._errorMessage);

                                                        _errorMessage = ExistsAttribute();
                                                        if (ErrorMessage != "") { this.Error = true; throw new Exception(_errorMessage); }
                                                        if (query.Contains("where"))
                                                        {
                                                            List<Item> items = FormatCondition(this._conditionTextAB);
                                                            //Check fuzzy set and object here
                                                            this.ErrorMessage = ExistsFuzzySet(items);
                                                            if (ErrorMessage != "")
                                                            {

                                                                ShowMessage(ErrorMessage, Color.Red); return;
                                                            }

                                                            QueryConditionBLL1 condition = new QueryConditionBLL1(items, this.rlAB);
                                                            resultAB.Scheme.Attributes = this._selectedAttributesAB;

                                                            foreach (FzTupleEntity tuple in this.rlAB.Tuples)
                                                            {
                                                                if (condition.Satisfy(items, tuple))
                                                                {
                                                                    if (this._selectedAttributeTextsAB != null)
                                                                        resultAB.Tuples.Add(GetSelectedAttributes(condition.ResultTuple));
                                                                    else
                                                                        resultAB.Tuples.Add(condition.ResultTuple);
                                                                }
                                                            }
                                                        }
                                                        else// Select all tuples
                                                        {
                                                            resultAB.Scheme.Attributes = this._selectedAttributesAB;
                                                            resultAB.RelationName = this.rlAB.RelationName;

                                                            if (this._selectedAttributeTextsAB != null)
                                                            {
                                                                foreach (var item in this.rlAB.Tuples)
                                                                    resultAB.Tuples.Add(GetSelectedAttributes(item));
                                                            }
                                                            else
                                                            {
                                                                foreach (var item in this.rlAB.Tuples)
                                                                    resultAB.Tuples.Add(item);
                                                            }
                                                        }

                                                        if (resultAB != null)
                                                        {
                                                            foreach (FzAttributeEntity attribute in resultAB.Scheme.Attributes)
                                                                GridViewResult.Columns.Add(attribute.AttributeName, attribute.AttributeName);

                                                            int j, i = -1;
                                                            foreach (FzTupleEntity tuple in resultAB.Tuples)
                                                            {
                                                                GridViewResult.Rows.Add();
                                                                i++; j = -1;
                                                                foreach (Object value in tuple.ValuesOnPerRow)
                                                                    GridViewResult.Rows[i].Cells[++j].Value = value.ToString();
                                                            }

                                                            xtraTabQueryResult.SelectedTabPageIndex = 0;
                                                        }
                                                        else
                                                        {
                                                            txtMessage.Text = "There is no relation satisfy the condition";
                                                            xtraTabQueryResult.SelectedTabPageIndex = 1;
                                                        }
                                                        siStatus.Caption = "Ready";
                                                        txtMessage.ForeColor = Color.Black;
                                                        txtMessage.Text = "This is decartes operation."+" There are " + GridViewResult.RowCount + " row(s) affected";

                                                    }
                                                    if (same_att == 1)
                                                    {
                                                        int ncol1 = resultA.Scheme.Attributes.Count;
                                                        int nrow1 = resultA.Tuples.Count;
                                                        int ncol2 = resultB.Scheme.Attributes.Count;
                                                        int nrow2 = resultB.Tuples.Count;
                                                        int m = 0;

                                                        bool same;
                                                        int stt = 0;
                                                        int sttvalue = 0;
                                                        int same1 = 0;
                                                        int same2 = 0;
                                                        int dem = 0;
                                                        int stt2;
                                                        int[] arrAB1 = new int[ncol1];
                                                        foreach (FzAttributeEntity attribute1 in resultA.Scheme.Attributes)
                                                        {

                                                            same = false;
                                                            stt++;
                                                            stt2 = 0;
                                                            foreach (FzAttributeEntity attribute2 in resultB.Scheme.Attributes)
                                                            {
                                                                stt2++;
                                                                if ((attribute1.AttributeName.ToString() == attribute2.AttributeName.ToString()))
                                                                {
                                                                    dem++;
                                                                    same = true;
                                                                    arrAB1[stt - 1] = 1;
                                                                    if (dem == 1)
                                                                    {
                                                                        same1 = stt;
                                                                        same2 = stt2;
                                                                    }
                                                                }
                                                               
                                                            }
                                                            if (same == false && (m < ncol1 - 2))
                                                            {
                                                                atAB.Add(attribute1);
                                                                m++;
                                                                arrAB1[stt - 1] = 0;
                                                            }
                                                            if (m == ncol1 - 2)
                                                            {
                                                                foreach (FzAttributeEntity attribute2 in resultB.Scheme.Attributes)
                                                                {
                                                                    atAB.Add(attribute2);
                                                                    m++;
                                                                }
                                                            }
                                                        }
                                                        rlAB.Scheme.Attributes = atAB;
                                                        int row = 0;

                                                        FzTupleEntity tpl1;
                                                        FzTupleEntity tpl2;
                                                        for (int i = 0; i < nrow1; i++)
                                                        {

                                                            tpl1 = resultA.Tuples[i];
                                                            for (int j = 0; j < nrow2; j++)
                                                            {
                                                                tpl2 = resultB.Tuples[j];
                                                                if (tpl1.ValuesOnPerRow[same1 - 1].ToString() == tpl2.ValuesOnPerRow[same2 - 1].ToString())
                                                                {
                                                                    List<Object> objs = new List<object>();

                                                                    int k = 0;
                                                                    int stt_cell;
                                                                    stt_cell = 0;
                                                                    foreach (Object value1 in tpl1.ValuesOnPerRow)
                                                                    {
                                                                        stt_cell++;

                                                                        if ((k < ncol1 - 2) && (arrAB1[stt_cell - 1] == 0))
                                                                        {
                                                                            objs.Add(value1.ToString());
                                                                            k++;
                                                                        }

                                                                    }
                                                                    foreach (Object value2 in tpl2.ValuesOnPerRow)
                                                                    {
                                                                        if ((k >= (ncol1 - 2)) && (k < m - 1))
                                                                        {
                                                                            objs.Add(value2.ToString());
                                                                            k++;
                                                                        }

                                                                    }
                                                                    if ((k == m - 1) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) >= Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                                    {

                                                                        objs.Add(tpl1.ValuesOnPerRow[ncol1 - 1]);
                                                                    }
                                                                    else
                                                                    {
                                                                        if ((k == m - 1) && (Convert.ToDouble(tpl1.ValuesOnPerRow[ncol1 - 1]) < Convert.ToDouble(tpl2.ValuesOnPerRow[ncol2 - 1])))
                                                                        {
                                                                            objs.Add(tpl2.ValuesOnPerRow[ncol2 - 1]);
                                                                        }

                                                                    }
                                                                    FzTupleEntity tplab = new FzTupleEntity() { ValuesOnPerRow = objs };
                                                                    rlAB.Tuples.Add(tplab);
                                                                    row++;

                                                                }
                                                            }
                                                        }
                                                        String queryAB = GetQueryAB(query) + " from " + rlAB.RelationName;
                                                        QueryPL.StandardizeQuery(queryAB);
                                                        QueryExcutetionBLL excutetionAB = new QueryExcutetionBLL(queryAB.ToLower(), newFdb.Relations);
                                                        FzRelationEntity resultAB = new FzRelationEntity();
                                                        this._selectedAttributeTextsAB = GetAttributeTextsAB(query);
                                                        this._conditionTextAB = GetConditionText(query);
                                                        if (_conditionTextAB != String.Empty)
                                                            this._conditionTextAB = AddParenthesis(this._conditionTextAB);
                                                        this.GetSelectedAttr();
                                                        if (this._error) throw new Exception(this._errorMessage);

                                                        _errorMessage = ExistsAttribute();
                                                        if (ErrorMessage != "") { this.Error = true; throw new Exception(_errorMessage); }
                                                        if (query.Contains("where"))
                                                        {
                                                            List<Item> items = FormatCondition(this._conditionTextAB);
                                                            //Check fuzzy set and object here
                                                            this.ErrorMessage = ExistsFuzzySet(items);
                                                            if (ErrorMessage != "")
                                                            {

                                                                ShowMessage(ErrorMessage, Color.Red); return;
                                                            }

                                                            QueryConditionBLL1 condition = new QueryConditionBLL1(items, this.rlAB);
                                                            resultAB.Scheme.Attributes = this._selectedAttributesAB;

                                                            foreach (FzTupleEntity tuple in this.rlAB.Tuples)
                                                            {
                                                                if (condition.Satisfy(items, tuple))
                                                                {
                                                                    if (this._selectedAttributeTextsAB != null)
                                                                        resultAB.Tuples.Add(GetSelectedAttributes(condition.ResultTuple));
                                                                    else
                                                                        resultAB.Tuples.Add(condition.ResultTuple);
                                                                }
                                                            }
                                                        }
                                                        else// Select all tuples
                                                        {
                                                            resultAB.Scheme.Attributes = this._selectedAttributesAB;
                                                            resultAB.RelationName = this.rlAB.RelationName;

                                                            if (this._selectedAttributeTextsAB != null)
                                                            {
                                                                foreach (var item in this.rlAB.Tuples)
                                                                    resultAB.Tuples.Add(GetSelectedAttributes(item));
                                                            }
                                                            else
                                                            {
                                                                foreach (var item in this.rlAB.Tuples)
                                                                    resultAB.Tuples.Add(item);
                                                            }
                                                        }

                                                        if (resultAB != null)
                                                        {
                                                            foreach (FzAttributeEntity attribute in resultAB.Scheme.Attributes)
                                                                GridViewResult.Columns.Add(attribute.AttributeName, attribute.AttributeName);

                                                            int j, i = -1;
                                                            foreach (FzTupleEntity tuple in resultAB.Tuples)
                                                            {
                                                                GridViewResult.Rows.Add();
                                                                i++; j = -1;
                                                                foreach (Object value in tuple.ValuesOnPerRow)
                                                                    GridViewResult.Rows[i].Cells[++j].Value = value.ToString();
                                                            }

                                                            xtraTabQueryResult.SelectedTabPageIndex = 0;
                                                        }
                                                        else
                                                        {
                                                            txtMessage.Text = "There is no relation satisfy the condition";
                                                            xtraTabQueryResult.SelectedTabPageIndex = 1;
                                                        }
                                                        siStatus.Caption = "Ready";
                                                        txtMessage.ForeColor = Color.Black;
                                                        txtMessage.Text = "This is natural join operation." + " There are " + GridViewResult.RowCount + " row(s) affected";


                                                    }
                                                    else
                                                    {
                                                        txtMessage.Text = "Between two ralations, there is only one the same attribute";
                                                        xtraTabQueryResult.SelectedTabPageIndex = 1;
                                                    }
                                                }

                                            }

                                            else
                                            {
                                                String message = null;
                                                message = QueryPL.CheckSyntax(query);
                                                if (message != "")
                                                {
                                                    ShowMessage(message, Color.Red);
                                                    return;
                                                }
                                                QueryExcutetionBLL excutetion = null;
                                                excutetion = new QueryExcutetionBLL(query.ToLower(), newFdb.Relations);
                                                FzRelationEntity result = null;
                                                result = excutetion.ExecuteQuery();
                                                if (excutetion.Error)
                                                {
                                                    ShowMessage(excutetion.ErrorMessage, Color.Red); return;
                                                }

                                                if (query.Contains("oder by"))
                                                {
                                                    List<FzTupleEntity> listtuple = new List<FzTupleEntity>();
                                                    //List<FzAttributeEntity> listattr = new List<FzAttributeEntity>();
                                                    List<String> AttrList = new List<String>();

                                                    String[] condition, AttrSelect = null;

                                                    int i = 7;//Attribute after "select"
                                                    int j = query.IndexOf("from");
                                                    String tmp1 = query.Substring(i, j - i);
                                                    tmp1 = tmp1.Replace(" ", "");
                                                    AttrSelect = tmp1.Split(',');

                                                    int left = query.IndexOf("oder by") + 7;//Attribute after "oder by"
                                                    int right;
                                                    if (query.Contains("desc")){
                                                         right = query.IndexOf("desc");
                                                    }
                                                    else
                                                    {
                                                        if (query.Contains("asc")){
                                                            right = query.IndexOf("asc");
                                                        }
                                                        else { right = query.Length; }
                                                    }
                                                    
                                                    String tmp = query.Substring(left, right - left);
                                                    tmp = tmp.Replace(" ", "");
                                                    condition = tmp.Split(',');

                                                    int indexofAttribute = 0;
                                                    for (int a = 0; a < condition.Length; a++ )
                                                    {
                                                        string temp = condition[a];
                                                        for (int b = 0; b < result.Scheme.Attributes.Count; b++)
                                                        {
                                                            if (result.Scheme.Attributes[b].AttributeName.ToString().ToLower().Contains(temp))
                                                            {
                                                                indexofAttribute = b;
                                                            }
                                                        }
                                                        for (int c = 0; c < result.Tuples.Count; c++)
                                                        {
                                                            AttrList.Add(result.Tuples[c].ValuesOnPerRow[indexofAttribute].ToString());                                                       
                                                        }
                                                    }
                                                    AttrList.Sort();
                                                    for (int d = 0; d < AttrList.Count - 1; d++)
                                                    {
                                                        if (AttrList[d] == AttrList[d + 1]) { AttrList.Remove(AttrList[d]); }
                                                    }

                                                    if (query.Contains("desc"))
                                                    {
                                                        for (int e = AttrList.Count - 1; e >= 0; e--)
                                                        {
                                                            string temp1 = AttrList[e];
                                                            for (int f = 0; f < result.Tuples.Count; f++)
                                                            {
                                                                if (AttrList[e].Equals(result.Tuples[f].ValuesOnPerRow[indexofAttribute]))
                                                                {
                                                                    listtuple.Add(result.Tuples[f]);
                                                                }                 
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        for (int e = 0; e < AttrList.Count; e++)
                                                        {
                                                            string temp = AttrList[e];
                                                            for (int f = 0; f < result.Tuples.Count; f++)
                                                            {
                                                                if (temp.Equals(result.Tuples[f].ValuesOnPerRow[indexofAttribute]))
                                                                {
                                                                    listtuple.Add(result.Tuples[f]);
                                                                }
                                                            }
                                                        }
                                                    }    
                                                    result.Tuples.Clear();

                                                    foreach (FzTupleEntity tuple in listtuple)
                                                    {
                                                        result.Tuples.Add(tuple);
                                                    }
    
                                                }

                                                if(query.Contains("top")) {
                                                    int left = 11;//Attribute after "select"
                                                    int right = query.IndexOf("from");
                                                    String numtop = query.Substring(left, right - left - 1);
                                                    if (int.Parse(numtop.ToLower().ToString()) > result.Tuples.Count)
                                                    {
                                                        numtop = result.Tuples.Count.ToString();
                                                    }
                                                    List<FzTupleEntity> listtuple = new List<FzTupleEntity>();
                                                    for (int i = 0; i < int.Parse(numtop.ToLower().ToString()); i++) {
                                                        listtuple.Add(result.Tuples[i]);  
                                                    }
                                                    result.Tuples.Clear();

                                                    foreach (FzTupleEntity tuple in listtuple) {
                                                        result.Tuples.Add(tuple);
                                                    }     
                                                    
                                                }

                                                if (query.Contains("max") || query.Contains("min") || query.Contains("sum") || query.Contains("avg"))
                                                {
                                                    int indexofAttr = 0;
                                                    int a = query.IndexOf("(");
                                                    int b = query.IndexOf(")");
                                                    string c = query.Substring(a, b - a);
                                                    c = c.Replace("(", "");
                                                    c = c.Replace(")", "");
                                                    c = c.Replace(" ", "");
                                                    
                                                    int indexofAttrSum = 0;    
                                                    int indexofAttrAvg = 0;
                                                    if (query.Contains("sum") || query.Contains("avg")) {
                                                         String[] AttrSumAvg = null;
                                                         int i = 7;//Attribute after "select"
                                                         int j = query.IndexOf("from");
                                                         String tmp1 = query.Substring(i, j - i);
                                                         tmp1 = tmp1.Replace(" ", "");
                                                         AttrSumAvg = tmp1.Split(',');
                                                         for (int ii = 0; ii < AttrSumAvg.Length; ii++)
                                                         {
                                                             if (AttrSumAvg[ii].Contains("sum")){
                                                                 indexofAttrSum = ii;
                                                                 if (AttrSumAvg[ii].Contains("max"))
                                                                 {
                                                                     c = c.Replace("max", "");
                                                                 }
                                                                 if (AttrSumAvg[ii].Contains("min"))
                                                                 {
                                                                     c = c.Replace("min", "");
                                                                 }
                                                             }
                                                             if (AttrSumAvg[ii].Contains("avg"))
                                                             {
                                                                 indexofAttrAvg = ii;
                                                             }
                                                         }
                                                    }

                                                    for (int j = 0; j < result.Scheme.Attributes.Count; j++)
                                                    {
                                                        if (result.Scheme.Attributes[j].AttributeName.ToString().ToLower().Contains(c))
                                                        {
                                                            indexofAttr = j;
                                                        }
                                                    }
                                                    double value = double.Parse(result.Tuples[0].ValuesOnPerRow[indexofAttr].ToString());
                                                    double membership = double.Parse(result.Tuples[0].ValuesOnPerRow[result.Scheme.Attributes.Count - 1].ToString());

                                                    double temp ;
                                                    for (int i = 1; i < result.Tuples.Count; i++)
                                                    {
                                                        temp = double.Parse(result.Tuples[i].ValuesOnPerRow[indexofAttr].ToString());
                                                        if (query.Contains("max"))
                                                        {
                                                            if (temp > value)
                                                            {
                                                                value = temp;
                                                            }
                                                        }

                                                        if (query.Contains("min"))
                                                        {
                                                            if (temp < value)
                                                            {
                                                                value = temp;
                                                            }
                                                        }
                                                        if (query.Contains("sum") || query.Contains("avg"))
                                                        {
                                                            if (query.Contains("max") || query.Contains("min"))
                                                            {
                                                                //value += temp;
                                                            }
                                                            else
                                                            {
                                                                value += temp;
                                                            }
                                                            if (membership > double.Parse(result.Tuples[i].ValuesOnPerRow[result.Scheme.Attributes.Count - 1].ToString()))
                                                            {
                                                                membership = double.Parse(result.Tuples[i].ValuesOnPerRow[result.Scheme.Attributes.Count - 1].ToString());
                                                            }
                                                        }
                                                    }

                                                    List<FzTupleEntity> listtuple = new List<FzTupleEntity>();

                                                    if (query.Contains("max") || query.Contains("min"))
                                                    {
                                                        for (int i = 0; i < result.Tuples.Count; i++)
                                                        {
                                                            if (double.Parse(result.Tuples[i].ValuesOnPerRow[indexofAttr].ToString()) == value)
                                                            {
                                                                listtuple.Add(result.Tuples[i]);
                                                            }
                                                        }
                                                    }
                                                    if (query.Contains("sum") || query.Contains("avg"))
                                                    {
                                                        if (query.Contains("sum"))
                                                        {
                                                            if (query.Contains("max") || query.Contains("min"))
                                                            {
                                                                result.Tuples[0].ValuesOnPerRow[indexofAttrSum] = value * listtuple.Count;
                                                            }
                                                            else
                                                            {
                                                                result.Tuples[0].ValuesOnPerRow[indexofAttrSum] = value;
                                                            }
                                                            
                                                        }
                                                        if (query.Contains("avg"))
                                                        {
                                                            if (query.Contains("max") || query.Contains("min"))
                                                            {
                                                                result.Tuples[0].ValuesOnPerRow[indexofAttrAvg] = value;
                                                            }
                                                            else
                                                            {
                                                                result.Tuples[0].ValuesOnPerRow[indexofAttrAvg] = value / result.Tuples.Count;
                                                            }
                                                        }
                                                        result.Tuples[0].ValuesOnPerRow[result.Scheme.Attributes.Count - 1] = membership;
                                                        listtuple.Clear();
                                                        listtuple.Add(result.Tuples[0]);
                                                        //return;
                                                    }
                                                    result.Tuples.Clear();
                                                    foreach (FzTupleEntity tuple in listtuple)
                                                    {
                                                        result.Tuples.Add(tuple);
                                                    } 
                                                           
                                                }

                                                if (result != null)
                                                {
                                                    foreach (FzAttributeEntity attribute in result.Scheme.Attributes)
                                                        GridViewResult.Columns.Add(attribute.AttributeName, attribute.AttributeName);

                                                    int j, i = -1;
                                                    foreach (FzTupleEntity tuple in result.Tuples)
                                                    {
                                                        GridViewResult.Rows.Add();
                                                        i++; j = -1;
                                                        foreach (Object value in tuple.ValuesOnPerRow)
                                                            GridViewResult.Rows[i].Cells[++j].Value = value.ToString();
                                                    }

                                                    xtraTabQueryResult.SelectedTabPageIndex = 0;
                                                }
                                                else
                                                {
                                                    txtMessage.Text = "There are no relation satisfy the condition";
                                                    xtraTabQueryResult.SelectedTabPageIndex = 1;
                                                }

                                                siStatus.Caption = "Ready";
                                                txtMessage.ForeColor = Color.Black;
                                                txtMessage.Text = "There are " + GridViewResult.RowCount + " row(s) affected";

                                            }
                                        }
                                        
                                       }
                                    }
                                }
                            }

                        }
                    }
                }


            }
            catch (Exception ex)
            {
                return;
                //MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }


        private void PrepareQuery()
        {
            GridViewResult.Rows.Clear();
            GridViewResult.Columns.Clear();
            txtMessage.Text = "";
            siStatus.Caption = "Executing query...";
        }

        private void Change(string a, string b) 
        {
            string temp = a;
            a = b;
            b = temp;
        }

        private void iExecuteQuery_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            ExecutingQuery();
        }

        private void iStopExecute_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            MessageBox.Show("Function is under contruction!");
        }
        #endregion

        #region 5. Fuzzy Set Ribbon Page

        private void iDiscreteFuzzySet_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmDescreteEditor frm = new frmDescreteEditor();
            frm.ShowDialog();
        }

        private void barButtonItem11_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmContinuousEditor frm = new frmContinuousEditor();
            frm.ShowDialog();
        }

        private void iListDescrete_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmListDescrete frm = new frmListDescrete();
            frm.ShowDialog();

            DrawChart(frm.PointList);
        }

        private void iListContinuous_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmListContinuous frm = new frmListContinuous();
            frm.ShowDialog();

            DrawChart(frm.PointList);
        }

        private void iGetMemberships_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmFuzzySetAction frm = new frmFuzzySetAction();
            frm.ShowDialog();
        }

        private void DrawChart(List<ContinuousFuzzySetBLL> conFS)
        {
            if (conFS == null || conFS.Count == 0) return;

            xtraTabDatabase.SelectedTabPageIndex = 3;
            chart1.ChartAreas.Clear();
            chart1.Series.Clear();
            Double max = int.MinValue;
            Color[] colors = GetUniqueRandomColor(conFS.Count);
            // Get max on Cordinate-X
            foreach (var item in conFS)
            {
                if (item.Bottom_Right > max)
                    max = item.Bottom_Right;
            }

            chart1.ChartAreas.Add("FuzzySets");
            chart1.ChartAreas["FuzzySets"].AxisX.Minimum = 0;
            chart1.ChartAreas["FuzzySets"].AxisX.Maximum = max;
            chart1.ChartAreas["FuzzySets"].AxisX.Interval = 5;
            chart1.ChartAreas["FuzzySets"].AxisY.Minimum = 0;
            chart1.ChartAreas["FuzzySets"].AxisY.Maximum = 2;
            chart1.ChartAreas["FuzzySets"].AxisY.Interval = 0.1;

            int i = 0;
            foreach (var item in conFS)
            {
                chart1.Series.Add(item.FuzzySetName);
                chart1.Series[item.FuzzySetName].Color = colors[i];
                chart1.Series[item.FuzzySetName].BorderWidth = 5;
                chart1.Series[item.FuzzySetName].Label = item.FuzzySetName;
                chart1.Series[item.FuzzySetName].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line;
                chart1.Series[item.FuzzySetName].Points.AddXY(item.Bottom_Left, 0);
                chart1.Series[item.FuzzySetName].Points.AddXY(item.Top_Left, 1);
                chart1.Series[item.FuzzySetName].Points.AddXY(item.Top_Right, 1);
                chart1.Series[item.FuzzySetName].Points.AddXY(item.Bottom_Right, 0);

                i++;
            }
        }

        private void DrawChart(List<DiscreteFuzzySetBLL> disFS)
        {
            if (disFS == null || disFS.Count == 0) return;

            xtraTabDatabase.SelectedTabPageIndex = 3;
            chart1.ChartAreas.Clear();
            chart1.Series.Clear();
            Double max = int.MinValue;
            Color[] colors = GetUniqueRandomColor(disFS.Count);
            // Get max on Cordinate-X
            foreach (var item in disFS)
            {
                Double tmp = item.GetMaxValue();
                if (tmp > max)
                    max = tmp;
            }

            chart1.ChartAreas.Add("FuzzySets");
            chart1.ChartAreas["FuzzySets"].AxisX.Minimum = 0;
            chart1.ChartAreas["FuzzySets"].AxisX.Maximum = max;
            chart1.ChartAreas["FuzzySets"].AxisX.Interval = 5;
            chart1.ChartAreas["FuzzySets"].AxisY.Minimum = 0;
            chart1.ChartAreas["FuzzySets"].AxisY.Maximum = 1;
            chart1.ChartAreas["FuzzySets"].AxisY.Interval = 0.1;

            int i = 0;
            foreach (var item in disFS)
            {
                chart1.Series.Add(item.FuzzySetName);
                chart1.Series[item.FuzzySetName].Color = colors[i];
                chart1.Series[item.FuzzySetName].BorderWidth = 2;
                chart1.Series[item.FuzzySetName].Label = item.FuzzySetName;
                chart1.Series[item.FuzzySetName].ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Point;
                for (int j = 0; j < item.ValueSet.Count; j++)
                {
                    chart1.Series[item.FuzzySetName].Points.AddXY(item.ValueSet[j], item.MembershipSet[j]);
                }

                i++;
            }
        }

        private Color[] GetUniqueRandomColor(int count)
        {
            Color[] colors = new Color[count];
            HashSet<Color> hs = new HashSet<Color>();
            Random randomColor = new Random();

            for (int i = 0; i < count; i++)
            {
                Color color;
                while (!hs.Add(color = Color.FromArgb(randomColor.Next(1, 100), randomColor.Next(1, 225), randomColor.Next(1, 230)))) ;
                colors[i] = color;
            }

            return colors;
        }

        #endregion

        #region 6. Help Ribbon Page
        #endregion

        #region 7. Navigation Pane (Tree List)

        private void ShowTreeList()
        {
            treeList1.Nodes.Clear();
            SetTreeListImageCollection();

            //Node fuzzy database
            parentFdbNode = new TreeNode();
            parentFdbNode.Text = DBValues.dbName.ToUpper();
            //NodeDB.ToolTipText = "Database " + Resource.dbShowName;
            parentFdbNode.ContextMenuStrip = ContextMenu_Database;
            parentFdbNode.ImageIndex = parentFdbImageTree.unselectedState;
            parentFdbNode.SelectedImageIndex = parentFdbImageTree.selectedState;
            treeList1.Nodes.Add(parentFdbNode);

            //Node scheme
            childSchemeNode = new TreeNode();
            childSchemeNode.Text = "Schemes";
            childSchemeNode.ToolTipText = "Schemes";
            childSchemeNode.ContextMenuStrip = ContextMenu_Schema;
            childSchemeNode.ImageIndex = folderImageTree.unselectedState;
            childSchemeNode.SelectedImageIndex = folderImageTree.selectedState;
            parentFdbNode.Nodes.Add(childSchemeNode);

            //Node Relation
            childRelationNode = new TreeNode();
            childRelationNode.Text = "Relations";
            childRelationNode.ToolTipText = "Relations";
            childRelationNode.ContextMenuStrip = ContextMenu_Relation;
            childRelationNode.ImageIndex = folderImageTree.unselectedState;
            childRelationNode.SelectedImageIndex = folderImageTree.selectedState;
            parentFdbNode.Nodes.Add(childRelationNode);

            //Node Query
            childQueryNode = new TreeNode();
            childQueryNode.Text = "Queries";
            childQueryNode.ToolTipText = "Queries";
            childQueryNode.ContextMenuStrip = ContextMenu_Query;
            childQueryNode.ImageIndex = folderImageTree.unselectedState;
            childQueryNode.SelectedImageIndex = folderImageTree.selectedState;
            parentFdbNode.Nodes.Add(childQueryNode);

        }

        private void ShowTreeListNode()
        {
            foreach (FzSchemeEntity s in fdbEntity.Schemes)
            {
                childNewNode = new TreeNode();
                childNewNode.Text = s.SchemeName;
                childNewNode.Name = s.SchemeName;
                childNewNode.ToolTipText = "Scheme " + s.SchemeName;
                childNewNode.ContextMenuStrip = ContextMenu_SchemaNode;
                childNewNode.ImageIndex = schemeImageTree.unselectedState;
                childNewNode.SelectedImageIndex = schemeImageTree.selectedState;
                childSchemeNode.Nodes.Add(childNewNode);
            }

            foreach (FzRelationEntity r in fdbEntity.Relations)
            {
                childNewNode = new TreeNode();
                childNewNode.Text = r.RelationName;
                childNewNode.Name = r.RelationName;
                childNewNode.ToolTipText = "Relation " + r.RelationName;
                childNewNode.ContextMenuStrip = ContextMenu_RelationNode;
                childNewNode.ImageIndex = relationImageTree.unselectedState;
                childNewNode.SelectedImageIndex = relationImageTree.selectedState;
                childRelationNode.Nodes.Add(childNewNode);
            }

            foreach (FzQueryEntity q in fdbEntity.Queries)
            {
                childNewNode = new TreeNode();
                childNewNode.Text = q.QueryName;
                childNewNode.Name = q.QueryName;
                childNewNode.ToolTipText = "Queries " + q.QueryName;
                childNewNode.ContextMenuStrip = ContextMenu_QueryNode;
                childNewNode.ImageIndex = queryImageTree.unselectedState;
                childNewNode.SelectedImageIndex = queryImageTree.selectedState;
                childQueryNode.Nodes.Add(childNewNode);
            }
        }

        private void SetTreeListImageCollection()
        {
            //treeList1.SelectImageList = treeListImageCollection;
            treeList1.ImageList = treeListImageCollection;

            parentFdbImageTree.selectedState = parentFdbImageTree.unselectedState = 0;
            folderImageTree.selectedState = 1;//folder is opened
            folderImageTree.unselectedState = 2;// folder is closed
            schemeImageTree.selectedState = schemeImageTree.unselectedState = 3;
            relationImageTree.selectedState = relationImageTree.unselectedState = 3;
            queryImageTree.selectedState = queryImageTree.unselectedState = 4;

        }

        private void TreeList1_NodeMouseClick(Object sender, TreeNodeMouseClickEventArgs e)
        {
            try
            {
                childCurrentNode = e.Node;

                if (childCurrentNode.Parent == parentFdbNode && !childCurrentNode.IsExpanded)
                {
                    e.Node.ImageIndex = e.Node.SelectedImageIndex = folderImageTree.unselectedState;
                }

                if (e.Button == MouseButtons.Right)
                {
                    childCurrentNode.ContextMenuStrip.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void TreeList1_AfterExpand(Object sender, TreeViewEventArgs e)
        {
            try
            {
                if (e.Node != parentFdbNode && e.Node.IsExpanded)
                {
                    e.Node.ImageIndex = e.Node.SelectedImageIndex = folderImageTree.selectedState;
                }

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void treeList1_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            childCurrentNode = e.Node;
            String name = childCurrentNode.Name;

            if (childCurrentNode.Parent == childRelationNode)
            {
                currentRelation = FzRelationBLL.GetRelationByName(name, fdbEntity);
                ShowColumnsAttribute(currentRelation);
                ShowTuples(currentRelation);
            }
            if (childCurrentNode.Parent == childSchemeNode)
            {
                currentScheme = FzSchemeBLL.GetSchemeByName(name, fdbEntity);
                OpenScheme(currentScheme);
            }
            if (childCurrentNode.Parent == childQueryNode)
            {
                currentQuery = FzQueryBLL.GetQueryByName(name, fdbEntity);
                OpenQuery(name);
                txtQuery.Focus();
            }

        }

        /// <summary>
        /// Delete tree node on treeList and also delete Object in db
        /// </summary>
        private void DeleteTreeNode(String deleteNodeName, FzSchemeEntity schemeEntity, FzRelationEntity relationEntity, FzQueryEntity queryEntity)
        {
            if (schemeEntity != null)
            {
                TreeNode deletedNode = childSchemeNode.Nodes[deleteNodeName];
                deletedNode.Remove();
                fdbEntity.Schemes.Remove(schemeEntity);
                schemeEntity = null;

                if (childSchemeNode.Nodes.Count == 0)
                {
                    childSchemeNode.ImageIndex = childSchemeNode.SelectedImageIndex = 2;
                }
            }

            if (relationEntity != null)
            {
                TreeNode deletedNode = childRelationNode.Nodes[deleteNodeName];
                deletedNode.Remove();
                fdbEntity.Relations.Remove(relationEntity);
                relationEntity = null;

                if (childRelationNode.Nodes.Count == 0)
                {
                    childRelationNode.ImageIndex = childRelationNode.SelectedImageIndex = 2;
                }
            }

            if (queryEntity != null)
            {
                TreeNode deletedNode = childQueryNode.Nodes[deleteNodeName];
                deletedNode.Remove();
                fdbEntity.Queries.Remove(queryEntity);
                queryEntity = null;

                if (childQueryNode.Nodes.Count == 0)
                {
                    childQueryNode.ImageIndex = childQueryNode.SelectedImageIndex = 2;//folder close
                }
            }
        }

        private void AddTreeNode() { }

        //private void ShowTreeList()
        //{
        //    treeList1.Nodes.Clear();
        //    SetTreeListImageCollection();   

        //    //Node database
        //    parentFdbNode = null;
        //    parentFdbNode = treeList1.AppendNode(null, null);
        //    parentFdbNode.SetValue("name", DBValues.dbName.ToUpper());//"name" is the "FileName" in Control TreeList (run desig and add column...)
        //    //
        //    parentFdbNode.ImageIndex = parentFdbImageTree.selectedState;
        //    parentFdbNode.SelectImageIndex = parentFdbImageTree.selectedState;

        //    //Node Scheme
        //    childSchemeNode = null;
        //    childSchemeNode = treeList1.AppendNode(null, parentFdbNode);
        //    childSchemeNode.SetValue("name", "Schemes");
        //    //Add context menu
        //    childSchemeNode.ImageIndex = folderImageTree.unselectedState;
        //    childSchemeNode.SelectImageIndex = folderImageTree.selectedState;

        //    //Node Relation
        //    childRelationNode = null;
        //    childRelationNode = treeList1.AppendNode(null, parentFdbNode);
        //    childRelationNode.SetValue("name", "Relations");
        //    //Add context menu
        //    childRelationNode.ImageIndex = folderImageTree.unselectedState;
        //    childRelationNode.SelectImageIndex = folderImageTree.selectedState;

        //    //Node Query
        //    childQueryNode = null;
        //    childQueryNode = treeList1.AppendNode(null, parentFdbNode);
        //    childQueryNode.SetValue("name", "Queries");
        //    //Add context menu
        //    childQueryNode.ImageIndex = folderImageTree.unselectedState;
        //    childQueryNode.SelectImageIndex = folderImageTree.selectedState;


        //}

        //private void ShowTreeListNode()
        //{

        //    foreach (FzSchemeEntity s in fdbEntity.Schemes)
        //    {
        //        childNewNode = null;
        //        childNewNode = treeList1.AppendNode(null, parentFdbNode);
        //        childNewNode.SetValue("name", s.SchemeName);
        //        //Add context menu here
        //        childNewNode.ImageIndex = schemeImageTree.unselectedState;
        //        childNewNode.SelectImageIndex = schemeImageTree.unselectedState;
        //    }

        //    foreach (FzRelationEntity r in fdbEntity.Relations)
        //    {
        //        childNewNode = null;
        //        childNewNode = treeList1.AppendNode(null, parentFdbNode);
        //        childNewNode.SetValue("name", r.RelationName);
        //        childNewNode.ImageIndex = relationImageTree.unselectedState;
        //        childNewNode.SelectImageIndex = relationImageTree.unselectedState;
        //    }

        //    foreach (FzQueryEntity q in fdbEntity.Queries)
        //    {
        //        childNewNode = null;
        //        childNewNode = treeList1.AppendNode(null, parentFdbNode);
        //        childNewNode.SetValue("name", q.QueryName);
        //        childNewNode.ImageIndex = queryImageTree.unselectedState;
        //        childNewNode.SelectImageIndex = queryImageTree.unselectedState;
        //    }
        //}

        //private IDXMenuManager menuManager;
        //public IDXMenuManager MenuManager
        //{
        //    get { return menuManager; }
        //    set { menuManager = value; }    
        //}

        //private DXPopupMenu CreatePopupMenu()
        //{
        //    DXPopupMenu menu = new DXPopupMenu();
        //    menu.Items.Add(new DXMenuItem("Menu Item 1"));
        //    return menu;
        //}

        //private void treeList1_CustomDrawNodeCell(object sender, DevExpress.XtraTreeList.CustomDrawNodeCellEventArgs e)
        //{
        //    TreeList tl = sender as TreeList;
        //    if (e.Node == tl.FocusedNode)
        //    {
        //        e.Graphics.FillRectangle(SystemBrushes.Window, e.Bounds);
        //        Rectangle rect = new Rectangle(
        //        e.EditViewInfo.ContentRect.Left,
        //        e.EditViewInfo.ContentRect.Top,
        //        Convert.ToInt32(e.Graphics.MeasureString(e.CellText, treeList1.Font).Width + 1),
        //        Convert.ToInt32(e.Graphics.MeasureString(e.CellText, treeList1.Font).Height));
        //        if ((sender as Control).Focused)
        //            e.Graphics.FillRectangle(SystemBrushes.Highlight, rect);
        //        else
        //            e.Graphics.FillRectangle(SystemBrushes.InactiveCaption, rect);
        //        e.Graphics.DrawString(e.CellText, treeList1.Font, SystemBrushes.HighlightText, rect);
        //        e.Handled = true;
        //    }
        //}

        #endregion

        #region 8. Start up Form

        private void InitSkinGallery()
        {
            SkinHelper.InitSkinGallery(rgbiSkins, true);
        }

        public void ResetObject()
        {
            fdbEntity = null;
            currentScheme = newScheme = null;
            //currentRelation = NewRelation = RenamedRelation = null;
            //CurrentQuery = NewQuery = RenamedQuery = null;
            parentFdbNode = childSchemeNode = childRelationNode = childQueryNode = childCurrentNode = childNewNode = null;
        }

        public void ResetSchemePage(Boolean state)
        {
            //CloseCurrentScheme();
            AddRowDefault();

            GridViewDesign.Enabled = state;
            Btn_Design_DeleteRow.Enabled = state;
            Btn_Design_ClearData.Enabled = state;
            Btn_Design_UpdateData.Enabled = state;
        }

        public void ResetRelationPage(bool state)
        {
            xtraTabDatabase.TabPages[1].Text = "Relation";

        }

        public void ResetQueryPage(bool state)
        {
            xtraTabDatabase.TabPages[2].Text = "Query";

        }

        public void ResetInputValue(bool state)
        {

        }

        private void ResetRibbonPage(Boolean state)
        {
            iSave.Enabled = state;
            iSaveAs.Enabled = state;
            iCloseDatabase.Enabled = state;

            connectionRibbonPageGroup.Visible = state;

            schemeRibbonPage.Visible = state;
            relationRibbonPage.Visible = state;
            queryRibbonPage.Visible = state;
        }

        private void ActiveDatabase(Boolean state)
        {
            ResetSchemePage(state);
            ResetRelationPage(state);
            ResetQueryPage(state);
            ResetInputValue(state);
            ResetRibbonPage(state);
        }

        private void StartApp()
        {
            currentRow = currentCell = 0;
            validated = flag = true;
            rollbackCell = false;
            xtraTabDatabase.Show();
            xtraTabDatabase.SelectedTabPageIndex = 0;

            //SwitchValueState(true);
            timer = new System.Timers.Timer(1000);
            timer.Elapsed += new ElapsedEventHandler(OnTimeEvent);
            ActiveDatabase(false);
        }

        #endregion

        #region 9. GridView Design Scheme
        private void AddRowDefault()
        {
            UnsetReadOnlyGridView();
            xtraTabDatabase.TabPages[0].Text = "Scheme";
            int n = GridViewDesign.Rows.Count - 2;
            for (int i = n; i >= 0; i--)
                //if (!GridViewDesign.Rows[i].IsNewRow)
                GridViewDesign.Rows.Remove(GridViewDesign.Rows[i]);
            Object[] _default = new Object[] { true, "Edit_PrimaryKey_Here", "Int32", "[-2147483648  ...  2147483647]", "The primary key of this relation" };
            GridViewDesign.Rows.Add(_default);
            toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = "2 / 2";
        }

        private void GridViewDesign_Click(object sender, EventArgs e)
        {
            if (GridViewDesign.CurrentRow != null)
            {
                toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = (GridViewDesign.CurrentRow.Index + 1) + " / " + GridViewDesign.Rows.Count;
            }
            else
            {
                toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = "1 / " + GridViewDesign.Rows.Count;
            }
        }

        private void GridViewDesign_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            if (GridViewDesign.CurrentRow != null)
            {
                toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = (GridViewDesign.CurrentRow.Index + 1) + " / " + GridViewDesign.Rows.Count;
            }
            else
            {
                toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = "1 / " + GridViewDesign.Rows.Count;
            }
        }

        private void GridViewDesign_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                if (flag) // set flag to prevent selectionChanged repeat 2 times
                {
                    if (GridViewDesign.CurrentRow.Index != currentRow)
                    {
                        if (ValidateRow(currentRow) == false)
                        {
                            flag = false;
                            GridViewDesign.CurrentCell = GridViewDesign.Rows[currentRow].Cells[currentCell];
                        }
                        else
                        {
                            // GridViewDesign.CurrentCell = GridViewDesign.Rows[currentRow].Cells[currentCell];
                            currentRow = GridViewDesign.CurrentRow.Index;
                        }
                    }
                }

                flag = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void GridViewDesign_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            try
            {
                if (currentScheme != null)
                {
                    if (e.ColumnIndex == 1)
                    {
                        if (FzSchemeBLL.IsInherited(currentScheme, fdbEntity.Relations))
                        {
                            MessageBox.Show("This scheme is read only!");
                            GridViewDesign.ClearSelection();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void GridViewDesign_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (GridViewDesign.CurrentCell.Value != null)
            {
                if (e.ColumnIndex == 1)
                {
                    for (int i = 0; i < GridViewDesign.Rows.Count - 1; i++)
                    {
                        if (GridViewDesign.CurrentCell.Value.ToString().CompareTo(GridViewDesign.Rows[i].Cells[1].Value.ToString()) == 0 && GridViewDesign.CurrentCell.RowIndex != i)
                        {
                            MessageBox.Show("There is already an attribute with the same name!");
                            GridViewDesign.ClearSelection();
                            GridViewDesign.CurrentCell.Selected = true;
                            break;
                        }
                    }
                }

                String temp = GridViewDesign.CurrentCell.Value.ToString();
                GridViewDesign.CurrentCell.ToolTipText = temp;
            }
        }

        private void GridViewDesign_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 0)
                {
                    if (currentScheme != null)
                    {
                        if (FzSchemeBLL.IsInherited(currentScheme, fdbEntity.Relations))
                        {
                            MessageBox.Show("This scheme is being inherited!");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void GridViewDesign_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.ColumnIndex == 2)
                {
                    if (currentScheme != null && FzSchemeBLL.IsInherited(currentScheme, fdbEntity.Relations))
                    {
                        MessageBox.Show("This scheme is being inherited!");
                    }
                    else
                    {
                        currentCell = e.ColumnIndex;

                        frmDataType frm = new frmDataType();
                        frm.ShowDialog();

                        if (frm.TypeName == "")
                        {
                            GridViewDesign.Rows[currentRow].Cells[currentCell].Value = frm.DataType;
                        }
                        else GridViewDesign.Rows[currentRow].Cells[currentCell].Value = frm.TypeName;

                        GridViewDesign.Rows[currentRow].Cells[currentCell + 1].Value = frm.Domain;
                    }
                }
                else if (e.ColumnIndex == 1)
                {
                    if (currentScheme != null && FzSchemeBLL.IsInherited(currentScheme, fdbEntity.Relations))
                    {
                        MessageBox.Show("This scheme is being inherited!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private bool ValidateRow(int rowIndex)
        {
            try
            {
                if (rowIndex >= 0 && rowIndex < GridViewDesign.Rows.Count)
                {
                    bool prKey = (GridViewDesign.Rows[rowIndex].Cells["PrimaryKey"].Value != null);
                    bool attrName = (GridViewDesign.Rows[rowIndex].Cells["ColumnName"].Value != null);
                    bool typeName = (GridViewDesign.Rows[rowIndex].Cells["ColumnType"].Value != null);
                    bool description = (GridViewDesign.Rows[rowIndex].Cells["ColumnDescription"].Value != null);
                    if (attrName && typeName)
                        return true;
                    else if (prKey || attrName || typeName || description)
                    {
                        if (!attrName && !typeName)
                        {
                            MessageBox.Show("Input the attribute name and data type!");
                            currentCell = 1;
                            return false;
                        }
                        else if (!attrName)
                        {
                            MessageBox.Show("Input the attribute name!");
                            currentCell = 1;
                            return false;
                        }
                        else
                        {
                            MessageBox.Show("Select a data type!");
                            currentCell = 2;
                            return false;
                        }
                    }
                }
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message);
            }

            return true;
        }

        private void btn_Design_Home_Click(object sender, EventArgs e)
        {
            if (GridViewDesign.Rows.Count > 1)
            {
                GridViewDesign.CurrentCell = GridViewDesign.Rows[0].Cells[0];
                toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = "1 / " + GridViewDesign.Rows.Count.ToString();
            }
        }

        private void btn_Design_Pre_Click(object sender, EventArgs e)
        {
            if (GridViewDesign.Rows.Count > 1)
            {
                int PreRow = GridViewDesign.CurrentRow.Index - 1;
                PreRow = (PreRow > 0 ? PreRow : 0);
                GridViewDesign.CurrentCell = GridViewDesign.Rows[PreRow].Cells[0];
                toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = (PreRow + 1).ToString() + " / " + GridViewDesign.Rows.Count.ToString();
            }
        }

        private void btn_Design_Next_Click(object sender, EventArgs e)
        {
            if (GridViewDesign.Rows.Count > 1)
            {
                int nRow = GridViewDesign.Rows.Count;
                int NextRow = GridViewDesign.CurrentRow.Index + 1;
                NextRow = (NextRow < nRow - 1 ? NextRow : nRow - 1);
                GridViewDesign.CurrentCell = GridViewDesign.Rows[NextRow].Cells[0];
                toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = (NextRow + 1).ToString() + " / " + GridViewDesign.Rows.Count.ToString();

            }
        }

        private void btn_Design_End_Click(object sender, EventArgs e)
        {
            if (GridViewDesign.Rows.Count > 1)
            {
                int nRow = GridViewDesign.Rows.Count;
                GridViewDesign.CurrentCell = GridViewDesign.Rows[nRow - 1].Cells[0];
                toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = nRow + " / " + nRow;
            }
        }

        private void Btn_Design_DeleteRow_Click(object sender, EventArgs e)
        {
            if (fdbEntity == null) return;
            if (GridViewDesign.Rows.Count > 1)
            {
                if (!GridViewDesign.Rows[currentRow].IsNewRow)
                {
                    GridViewDesign.Rows.Remove(GridViewDesign.CurrentRow);
                }

                toolStripLabel1.Text = lblDesignRowNumberIndicator.Text = GridViewDesign.CurrentRow.Index + 1 + " / " + GridViewDesign.Rows.Count;
            }
        }

        private void Btn_Design_ClearData_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = new DialogResult();
                result = MessageBox.Show("Clear all attributes data ?", "Clear All Data", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    //CloseCurrentScheme();
                    AddRowDefault();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void Btn_Design_UpdateData_Click(object sender, EventArgs e)
        {
            SaveScheme();
        }

        #endregion

        #region 10. GridView Data Relation
        private void UpdateDataRowNumber()
        {
            try
            {
                if (GridViewData.Rows.Count == 0)
                {
                    lblDataRowNumberIndicator.Text = "0 / 0";
                }
                else if (GridViewData.CurrentRow != null)
                {
                    lblDataRowNumberIndicator.Text = (GridViewData.CurrentRow.Index + 1) + " / " + GridViewData.Rows.Count;
                }
                else lblDataRowNumberIndicator.Text = "1 / " + GridViewData.Rows.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show("ERROR:\n" + ex.Message);
            }
        }

        private void btn_Data_Home_Click(object sender, EventArgs e)
        {
            if (GridViewData.Rows.Count > 1)// && !GridViewData.Rows[GridViewData.Rows.Count - 1].IsNewRow)
            {
                GridViewData.CurrentCell = GridViewData.Rows[0].Cells[0];
                lblDataRowNumberIndicator.Text = "1 / " + GridViewData.Rows.Count;
            }
        }

        private void btn_Data_Pre_Click(object sender, EventArgs e)
        {
            if (GridViewData.Rows.Count > 0)//&& !GridViewData.Rows[GridViewData.Rows.Count - 1].IsNewRow)
            {
                int PreRow = GridViewData.CurrentRow.Index - 1;
                PreRow = (PreRow > 0 ? PreRow : 0);
                GridViewData.CurrentCell = GridViewData.Rows[PreRow].Cells[0];
                lblDataRowNumberIndicator.Text = (PreRow + 1) + " / " + GridViewData.Rows.Count;
            }
        }

        private void btn_Data_Next_Click(object sender, EventArgs e)
        {
            if (GridViewData.Rows.Count > 0)
            {
                int nRow = GridViewData.Rows.Count;
                int NextRow = GridViewData.CurrentRow.Index + 1;
                NextRow = (NextRow < nRow - 1 ? NextRow : nRow - 1);
                GridViewData.CurrentCell = GridViewData.Rows[NextRow].Cells[0];
                lblDataRowNumberIndicator.Text = (NextRow + 1) + " / " + GridViewData.Rows.Count;
            }
        }

        private void btn_Data_End_Click(object sender, EventArgs e)
        {
            if (GridViewData.Rows.Count > 0)
            {
                int nRow = GridViewData.Rows.Count;
                GridViewData.CurrentCell = GridViewData.Rows[nRow - 1].Cells[0];
                lblDataRowNumberIndicator.Text = nRow + " / " + nRow;
            }
        }

        private void Btn_Data_DeleteRow_Click(object sender, EventArgs e)
        {
            if (GridViewData.Rows.Count > 1 && !GridViewData.CurrentRow.IsNewRow)
            {
                GridViewData.Rows.Remove(GridViewData.CurrentRow);
                UpdateDataRowNumber();
            }
        }

        private void Btn_Data_ClearData_Click(object sender, EventArgs e)
        {
            DialogResult result = new DialogResult();
            result = MessageBox.Show("Are you sure want to clear all data?", "Clear All Data", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                int n = GridViewData.Rows.Count - 2;
                for (int i = n; i >= 0; i--)
                    if (!GridViewData.Rows[i].IsNewRow)
                        GridViewData.Rows.Remove(GridViewData.Rows[i]);
                UpdateDataRowNumber();
            }
        }

        private void Btn_Data_UpdateData_Click(object sender, EventArgs e)
        {
            SaveRelation();
        }

        private void GridViewData_Click(object sender, EventArgs e)
        {
            UpdateDataRowNumber();
        }

        private void GridViewData_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            UpdateDataRowNumber();
        }

        private void GridViewData_SelectionChanged(object sender, EventArgs e)
        {
            if (rollbackCell)
            {
                GridViewData.CurrentCell = GridViewData.Rows[currentRow].Cells[currentCell];
            }
            rollbackCell = false;
        }

        private void GridViewData_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            GridViewData.CommitEdit((DataGridViewDataErrorContexts.Commit));
        }

        private void GridViewData_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            currentCell = e.ColumnIndex;
            currentRow = e.RowIndex;
            try
            {
                if (GridViewData.CurrentCell.Value != null)
                {
                    var value = GridViewData.CurrentCell.Value.ToString();//.ToString();

                    ///Convert value of current cell to correct datatype of attribute
                    ///If cannot convert, focus to current cell and block focus to others cell
                    if (currentRelation != null && !FzDataTypeBLL.CheckDataType(value, currentRelation.Scheme.Attributes[currentCell].DataType))
                    {
                        e.Cancel = true;
                        MessageBox.Show("Attribute value does not match with the data type!");
                        return;
                    }
                    if (!CheckPrimaryKey(e.RowIndex))
                    {
                        e.Cancel = true;
                        return;
                    }
                    if (!CheckMembership())
                    {
                        e.Cancel = true;

                        return;
                    }

                }
                else if (!GridViewData.Rows[GridViewData.Rows.Count - 1].IsNewRow)
                {
                    e.Cancel = true;
                    MessageBox.Show("Value can not be NULL!", "INFORM");
                }
            }
            catch (Exception ex) { }
        }

        private void GridViewData_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            //try
            //{
            //    currentCell = e.ColumnIndex;
            //    currentRow = e.RowIndex;

            //    if (GridViewData.CurrentCell.Value != null)
            //    {
            //        var value = GridViewData.CurrentCell.Value.ToString();//.ToString();

            //        ///Convert value of current cell to correct datatype of attribute
            //        ///If cannot convert, focus to current cell and block focus to others cell
            //        if (!FzDataTypeBLL.CheckDataType(value, currentRelation.Scheme.Attributes[currentCell].DataType))
            //        {
            //            MessageBox.Show("Attribute value does not match with the data type!");
            //            return;
            //        }
            //    }
            //    else
            //    {
            //        throw new Exception("NULL!");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show("ERROR:\n" + ex.Message);
            //    rollbackCell = true;
            //}
        }

        private Boolean CheckMembership()
        {
            int n = GridViewData.Rows.Count - 1;
            for (int i = 0; i < n; i++)
            {
                //Datatype has been checked above
                Double value = Double.Parse(GridViewData.Rows[i].Cells[GridViewData.ColumnCount - 1].Value.ToString());
                if (value > 1 || value <= 0)
                {
                    MessageBox.Show("The membership value at row " + (i + 1) + " must be between (0-1]");
                    return false;
                }
            }

            return true;
        }

        private Boolean CheckPrimaryKey(int row)//Current relation only allow one primarykey
        {
            List<int> indexPrm = FzRelationBLL.GetArrPrimaryKey(currentRelation);
            string value = GridViewData.Rows[row].Cells[indexPrm[0]].Value.ToString();
            for (int i = 0; i < GridViewData.Rows.Count - 1; i++)
            {
                if (value == GridViewData.Rows[i].Cells[indexPrm[0]].Value.ToString() && i != row)
                {
                    MessageBox.Show("The primary value must be unique");
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region 11. Help and About
        private void barButtonItem13_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

            About frm = new About();
            frm.ShowDialog();
        }

        private void iHelp_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            frmHelp frm = new frmHelp();
            frm.ShowDialog();
        }
        #endregion

        private void txtQuery_TextChanged(object sender, EventArgs e)
        {

        }

    }
}