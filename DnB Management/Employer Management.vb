Option Explicit On
Imports System.Net.Mail
Imports System.IO
Imports GlobalLibrary
Imports System.Data.SqlClient
Public Class Form1


#Region "Declarations for Global Library dll"
    '*** the following lines are for the developer to perform troubleshooting/testing on HLSV4
    '*** this will override the CN settings in the Global Library dll
    'Private Development As New GlobalLibrary.Development("Testing")
    'Private Development As New GlobalLibrary.Development("DataRepository")

    Private DBParameters As New GlobalLibrary.DBParameters(Enums.DatabaseMode.Development, "NASPROSQL1")
    Private Functions As New GlobalLibrary.Functions
    Private SQLHelper As New GlobalLibrary.SqlHelper
    Private usrApplicationManagment As New GlobalLibrary.ApplicationAccess.DRUser()
    Private CN As String = DBParameters.CN
    Private cnSQL As SqlClient.SqlConnection = New SqlClient.SqlConnection(DBParameters.CN)
#End Region


    Private filePath As String
    Private bInitial As Boolean
    Private strCurrentUser As String
    Public Userid As String

    Private dsNewCompanies As DataSet
    Private dsDBGlobal As DataSet
    Private dsoldDBGlobal As DataSet

    Private dsDemotionList As DataSet
    Private dsChangeRecords As DataSet
    Private dsCorporateList As DataSet
    Private dsCorpforSubsidiaryList As DataSet

    Private dsSubsidiaryList As DataSet
    Private dsSubDemotionList As DataSet
    Private dsStats As DataSet
    Private dsSubChangeRecords As DataSet
    Dim path As String = "\\nasprosql1\Dunn & Bradstreet\DnBFlow.sql"





    Private Sub Form1_Load(sender As Object, e As System.EventArgs) Handles Me.Load
        'Command() is the value of whatever parameters are passed to a VB windows application in the form
        'of one big long string with user defined delimeters
        'by performing a split command we can separate out the passed parameters
        Dim strParameters() As String = Split(Command(), "|")
        If strParameters.Length < 1 Or strParameters(0) = "" Then
            '*** the following commented lines are used for testing purposes
            'Mode = "Update"
            'MyDate = 8583
            'TextDate = "July 1, 2013"
            'MsgBox("You have entered outside of the Dashboard or there was an error loading")
            'Me.Close()
            'Exit Sub
        Else
            'Mode = strParameters(0)
            'MyDate = strParameters(1)
            'TextDate = strParameters(2)
        End If

        Userid = ApplicationAccess.DRUser.UserID

        'mark the fact that we aren't in production
        Functions.ResetBackColorOnView(Me, DBParameters.Backcolor)

        Select Case DBParameters.DatabaseMode
            Case Enums.DatabaseMode.Production
                filePath = "\\NASPROSQL1\HLI_Applications\Dashboard"
            Case Enums.DatabaseMode.Development
                filePath = "\\NASPROSQL1\HLI_Applications\Testing"
            Case Enums.DatabaseMode.Testing
                filePath = "\\NASPROSQL1\HLI_Applications\Testing"
            Case Enums.DatabaseMode.Local
                filePath = "\\NASPROSQL1\HLI_Applications\Testing"
        End Select

        Try
            bInitial = True
            'set the user access
            'If Not ApplicationAccess.DRUser.HasAccess("HIX_Maintenance", "Access") Then
            '    Functions.Sendmail("Entry Denial", "Form Load", "", MyDate, "HIX_Maintenance")
            '    MsgBox("You do not have permission to access this application")
            '    Me.Close()
            '    Exit Sub
            'End If
            strCurrentUser = ApplicationAccess.DRUser.UserID
            'Me.Caption = "Health Insurance Exchange Management Tool      " _
            '        & "           Welcome " _
            '        & ApplicationAccess.DRUser.UserProperties("FirstName") + " " _
            '        & ApplicationAccess.DRUser.UserProperties("LastName")


            'Me.DatabaseMode = DBParameters.DatabaseMode.ToString
            'Me.DatabaseName = DBParameters.databaseName
            'Me.ServerName = DBParameters.ServerName
            'Me.Version = String.Format("Version: {1}.{0}.{0}.{0}", My.Application.Info.Version.Build, My.Application.Info.Version.Major, My.Application.Info.Version.Minor, My.Application.Info.Version.MinorRevision)
            Me.BackColor = DBParameters.Backcolor

            'dsNewCompanies = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "DnB.Get_New_DUNS")
            'cmbNewGlobals.DataSource = dsNewCompanies.Tables(0)
            'cmbNewGlobals.DisplayMember = dsNewCompanies.Tables(0).Columns("Name").ToString

            'dsoldDBGlobal = SQLHelper.ExecuteDataset(CN, "DnB.s_Get_Dropped_DUNS")
            'cmbDroppedCompanies.DataSource = dsoldDBGlobal.Tables(0)
            'cmbDroppedCompanies.DisplayMember = dsoldDBGlobal.Tables(0).Columns("Name").ToString

            dsCorporateList = SQLHelper.ExecuteDataset(CN, "emp.s_get_Corporate_list")
            dgvCorporate_FormatGrid()
            dgvCorporate_BindData()
            dgvSubsidiaryBrowser_FormatGrid()

            dsCorpforSubsidiaryList = SQLHelper.ExecuteDataset(CN, "emp.s_Get_Full_List")
            cmbSubsidiaryBrowser.DataSource = dsCorpforSubsidiaryList.Tables(0)
            cmbSubsidiaryBrowser.DisplayMember = dsCorpforSubsidiaryList.Tables(0).Columns("Business Name").ToString

            dsStats = SQLHelper.ExecuteDataset(CN, "emp.s_Get_Stats")
            fillstats()

        
            TabControl1.Controls.Remove(TabControl1.TabPages("TabPage4"))
            Label44.Visible = False
            Label46.Visible = False
            Label47.Visible = False

            ToolTip1.SetToolTip(radDemotion, "A demotion means that the former Corporate record is now a Subsidiary and in the tier structure, it is second. (One record is above it)")
            ToolTip1.SetToolTip(radDoubleDemotion, "A double demotion means that the former Corporate record is now a Subsidiary and that in the tier structure it is now third. (Two records is above it)")
            ToolTip1.SetToolTip(radPromotion, "A promotion occurs when a company goes from subsidiary to the top company")
            ToolTip1.SetToolTip(btnOrphanedHolding, "Delete the Orphaned Holding Companies mentioned below")
            ToolTip1.SetToolTip(btnMatchSubDelete, "This will ateempt to verify that a possible delete has a matching Addition (Duns change without DnB knowing it) or that previously, there were duplicates with different DUNS.")
            ToolTip1.SetToolTip(btnAcceptAllChanges, "All subsidiares for this Coprporation that are slated for delete are deleted along with related data")
            ToolTip1.SetToolTip(btnRejectAllChanges, "All subsidiares for this Coprporation that are slated for delete are changed to 'No Change' so they won't be deleted.")
            ToolTip1.SetToolTip(ckbExpandedList, "Expands the list to include companies where employees here is greater than 100")

            bInitial = False
        Catch ex As Exception
            Functions.Sendmail(ex.Message, "Form Load", 0, 0, "DnB Management")
            MsgBox(ex.Message)
        End Try
    End Sub

    Private Sub fillstats()
        Try
            TextBox1.Text = isnull(dsStats.Tables(0).Rows(0).Item("Total Employers"))
            TextBox2.Text = isnull(dsStats.Tables(0).Rows(0).Item("Corporate Demotion"))
            TextBox3.Text = isnull(dsStats.Tables(0).Rows(0).Item("Corporate Double Demotion"))
            TextBox4.Text = isnull(dsStats.Tables(0).Rows(0).Item("Corporate Promotion"))
            TextBox5.Text = isnull(dsStats.Tables(0).Rows(0).Item("Corporate Addition"))
            TextBox6.Text = isnull(dsStats.Tables(0).Rows(0).Item("Corporate Delete"))
            TextBox7.Text = isnull(dsStats.Tables(0).Rows(0).Item("Subsidiary Demotion"))
            TextBox8.Text = isnull(dsStats.Tables(0).Rows(0).Item("Subsidiary Double Demotion"))
            TextBox9.Text = isnull(dsStats.Tables(0).Rows(0).Item("Subsidiary Promotion"))
            TextBox10.Text = isnull(dsStats.Tables(0).Rows(0).Item("Subsidiary Addition"))
            TextBox11.Text = isnull(dsStats.Tables(0).Rows(0).Item("Subsidiary Delete"))
            TextBox12.Text = isnull(dsStats.Tables(0).Rows(0).Item("Corporations"))
            TextBox13.Text = isnull(dsStats.Tables(0).Rows(0).Item("Subsidiaries"))
            TextBox14.Text = isnull(dsStats.Tables(0).Rows(0).Item("Oldest Corp"))
            TextBox93.Text = isnull(dsStats.Tables(0).Rows(0).Item("Newest Corp"))
            TextBox94.Text = isnull(dsStats.Tables(0).Rows(0).Item("Oldest Branch"))
            TextBox95.Text = isnull(dsStats.Tables(0).Rows(0).Item("Newest Branch"))
            TextBox96.Text = isnull(dsStats.Tables(0).Rows(0).Item("Corporate No Change"))
            TextBox97.Text = isnull(dsStats.Tables(0).Rows(0).Item("Corporate Update"))
            TextBox98.Text = isnull(dsStats.Tables(0).Rows(0).Item("Subsidiary No Change"))
            TextBox99.Text = isnull(dsStats.Tables(0).Rows(0).Item("Subsidiary Update"))

        Catch ex As Exception

        End Try
    End Sub
    'Private Sub Clear_NewGlobal()
    '    TextBox1.Clear()
    '    TextBox2.Clear()
    '    TextBox3.Clear()
    '    TextBox4.Clear()
    '    TextBox5.Clear()
    '    TextBox6.Clear()
    '    TextBox7.Clear()
    '    TextBox8.Clear()
    '    TextBox9.Clear()

    'End Sub

    'Private Sub Clear_DroppedGlobal()
    '    TextBox10.Clear()
    '    TextBox11.Clear()
    '    TextBox12.Clear()
    '    TextBox13.Clear()
    '    TextBox14.Clear()


    'End Sub

#Region "Combo"

    'Private Sub cmbNewGlobals_SelectedIndexChanged(sender As Object, e As System.EventArgs)
    '    Try
    '        If Not bInitial Then
    '            bInitial = True

    '            If cmbNewGlobals.SelectedIndex > 0 Then
    '                Clear_NewGlobal()

    '                dsDBGlobal = SQLHelper.ExecuteDataset(CN, "DnB.s_get_DBGlobalInfo", cmbNewGlobals.SelectedValue(0))

    '                TextBox1.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Name"))
    '                TextBox2.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("address"))
    '                TextBox3.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("City"))
    '                TextBox4.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("State"))
    '                TextBox5.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Zip"))
    '                TextBox6.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Duns"))
    '                TextBox7.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Country"))
    '                TextBox8.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Employees"))
    '                TextBox9.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Number of Family Members Global"))


    '            End If

    '            bInitial = False
    '        End If

    '    Catch ex As Exception
    '        Functions.Sendmail(ex.Message, "cmbNewGlobals_SelectedIndexChanged ", cmbNewGlobals.SelectedValue(1), 0, "Employer Maintenance")
    '        MsgBox("Employer Maintenance : cmbNewGlobals_SelectedIndexChanged : " + cmbNewGlobals.SelectedValue(1) + " : " + ex.Message)
    '    End Try
    'End Sub

    'Private Sub cmbDroppedCompanies_SelectedIndexChanged(sender As System.Object, e As System.EventArgs)

    '    Try
    '        If Not bInitial Then
    '            bInitial = True

    '            If cmbDroppedCompanies.SelectedIndex > 0 Then
    '                Clear_DroppedGlobal()


    '                dsDBGlobal = SQLHelper.ExecuteDataset(CN, "DnB.s_get_droppedDBGlobalInfo", cmbDroppedCompanies.SelectedValue(0))

    '                ''TextBox1.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Name"))
    '                ''TextBox2.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("address"))
    '                ''TextBox3.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("City"))
    '                ''TextBox4.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("State"))
    '                ''TextBox5.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Zip"))
    '                ''TextBox6.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Duns"))
    '                ''TextBox7.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Country"))
    '                ''TextBox8.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Employees"))
    '                ''TextBox9.Text = isnull(dsDBGlobal.Tables(0).Rows(0).Item("Number of Family Members Global"))


    '            End If

    '            bInitial = False
    '        End If

    '    Catch ex As Exception
    '        'Functions.Sendmail(ex.Message, "cmbDroppedCompanies_SelectedIndexChanged ", cmbDroppedCompanies.SelectedValue(1), 0, "Employer Maintenance")
    '        MsgBox("Employer Maintenance : cmbDroppedCompanies_SelectedIndexChanged : " + cmbDroppedCompanies.SelectedValue(1) + " : " + ex.Message)
    '    End Try
    'End Sub

    Private Sub radDemotion_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles radDemotion.CheckedChanged

        If radDemotion.Checked And Not bInitial Then
            Label44.Visible = True
            Label46.Visible = False
            Label47.Visible = False

            ToolTip1.SetToolTip(btnAccepttheChange, "Accepting the demotion Moves the Company from the Corporate list to the Subsidiary/branch list.  The company highlighted in red " _
                                                    + "becomes the new corparate.  DUNS hierarchy is maintained.")
            ToolTip1.SetToolTip(btnRejectChange, "Rejecting the demotion resets the ParentID for this company to itself and marks it to be left alone.")
            Try
                ' change combobox to be filled with Demotion list.
                bInitial = True
                Reprocess_Change_List()
                bInitial = False
            Catch ex As Exception
                bInitial = False
                'Functions.Sendmail(ex.Message, "radDemotion_CheckedChanged ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
                MsgBox("Employer Maintenance : radDemotion_CheckedChanged : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
            End Try

        End If
    End Sub

    Private Sub cmbChangeList_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cmbChangeList.SelectedIndexChanged
        Try
            If Not bInitial And cmbChangeList.SelectedIndex > 0 Then
                bInitial = True
                get_ChangeData()
                bInitial = False
            End If
        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "cmbChangeList_SelectedIndexChanged ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : cmbChangeList_SelectedIndexChanged : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

#End Region

#Region "Functions"

    Public Function isnull(ByVal Record As System.Object) As String
        If IsDBNull(Record) Then Return ""
        Return Record
    End Function


#End Region

#Region "Buttons"
    'Private Sub btnAcceptNewDuns_Click(sender As System.Object, e As System.EventArgs)
    '    Dim iresult As Integer
    '    Try
    '        iresult = SQLHelper.ExecuteScalar(CN, "DnB.s_Accept_New_Company", _
    '                                                IIf(cmbNewGlobals.SelectedValue(0) = 0, 0, CInt(cmbNewGlobals.SelectedValue(0).ToString)))

    '        bInitial = True

    '        Clear_NewGlobal()

    '        dsNewCompanies = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "DnB.Get_New_DUNS")
    '        cmbNewGlobals.DataSource = dsNewCompanies.Tables(0)
    '        cmbNewGlobals.DisplayMember = dsNewCompanies.Tables(0).Columns("Name").ToString

    '        cmbNewGlobals.SelectedIndex = 0

    '        bInitial = False

    '    Catch ex As Exception
    '        Functions.Sendmail(ex.Message, "btnAcceptNewDuns_Click ", cmbNewGlobals.SelectedValue(1), 0, "Employer Maintenance")
    '        MsgBox("Employer Maintenance : btnAcceptNewDuns_Click : " + cmbNewGlobals.SelectedValue(1) + " : " + ex.Message)
    '    End Try
    'End Sub

    'Private Sub btnRejectNewDuns_Click(sender As System.Object, e As System.EventArgs)
    '    Dim iresult As Integer
    '    Try
    '        iresult = SQLHelper.ExecuteScalar(CN, "DnB.s_Reject_New_Company", _
    '                                                IIf(cmbNewGlobals.SelectedValue(0) = 0, 0, CInt(cmbNewGlobals.SelectedValue(0).ToString)))

    '        bInitial = True

    '        Clear_NewGlobal()

    '        dsNewCompanies = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "DnB.Get_New_DUNS")
    '        cmbNewGlobals.DataSource = dsNewCompanies.Tables(0)
    '        cmbNewGlobals.DisplayMember = dsNewCompanies.Tables(0).Columns("Name").ToString

    '        cmbNewGlobals.SelectedIndex = 0

    '        bInitial = False

    '    Catch ex As Exception
    '        'Functions.Sendmail(ex.Message, "btnRejectNewDuns_Click ", cmbNewGlobals.SelectedValue(1), 0, "Employer Maintenance")
    '        MsgBox("Employer Maintenance : btnRejectNewDuns_Click : " + cmbNewGlobals.SelectedValue(1) + " : " + ex.Message)
    '    End Try
    'End Sub

    Private Sub btnAccepttheChange_Click(sender As System.Object, e As System.EventArgs) Handles btnAccepttheChange.Click
        Dim iresult As Integer
        Dim indexpointer As Int16
        Try
            bInitial = True
            indexpointer = cmbChangeList.SelectedIndex



            If radDemotion.Checked Or radDoubleDemotion.Checked Then
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_Demotion", cmbChangeList.SelectedValue(0), strCurrentUser)
                'objWriter.WriteLine("EMP.s_Accept_Demotion " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_Demotion " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using
            ElseIf radPromotion.Checked Then
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_Promotion", cmbChangeList.SelectedValue(0), strCurrentUser)
                'objWriter.WriteLine("EMP.s_Accept_Promotion " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_Promotion " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using
            ElseIf radDeletion.Checked Then
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_Delete", cmbChangeList.SelectedValue(0), strCurrentUser)
                'objWriter.WriteLine("EMP.s_Accept_Delete " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_Delete " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using
            ElseIf radAddition.Checked Then
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_Addition", cmbChangeList.SelectedValue(0), strCurrentUser)
                'objWriter.WriteLine("EMP.s_Accept_Delete " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_Addition " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using
            End If

            If iresult = 0 Then
                Reprocess_Change_List()
            End If

            'If dsChangeRecords.Tables(0).Rows.Count >= indexpointer + 1 Then
            '    cmbChangeList.SelectedIndex = indexpointer
            'Else
            '    cmbChangeList.SelectedIndex = 1
            'End If
            bInitial = False

        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnAccepttheChange_Click ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAccepttheChange_Click : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnRejectChange_Click(sender As System.Object, e As System.EventArgs) Handles btnRejectChange.Click
        Dim iresult As Integer
        Try
            bInitial = True
            If radAddition.Checked And cmbChangeList.SelectedIndex >= 0 Then
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Reject_Addition", cmbChangeList.SelectedValue(0), strCurrentUser)
                If iresult = 0 Then
                    Using sw As StreamWriter = File.AppendText(path)
                        sw.WriteLine("EMP.s_Reject_Addition " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                        sw.WriteLine("Go")
                    End Using
                End If
            Else
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Reject_Demotion", cmbChangeList.SelectedValue(0), strCurrentUser)
                If iresult = 0 Then
                    Using sw As StreamWriter = File.AppendText(path)
                        sw.WriteLine("EMP.s_Reject_Demotion " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                        sw.WriteLine("Go")
                    End Using
                End If
            End If

            Reprocess_Change_List()
            bInitial = False

        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnRejectChange_Click ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnRejectChange_Click : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub
#End Region


#Region "DataGrids"

    Private Sub dgvCorporate_FormatGrid()
        'This is a general formatting grid subroutine for the HMO, PPO, HMOMedicare, HMOMedicaid datagrids
        Try
            'set Visual Basic Datagrid Header style to false so we can use our own
            'The key statement required to get the column and row styles to work
            'Visual Header styles must be shut off
            dgvCorporate.EnableHeadersVisualStyles = False
            'go and set the styles
            With dgvCorporate
                'the following line is necessary for manual column sizing 
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
                'let the columns size their heights on their own
                .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
                '*** header settings
                'header backcolor, text color, font bold, font, multiline and alignment
                Dim columnHeaderStyle As New DataGridViewCellStyle
                columnHeaderStyle.BackColor = Color.FromArgb(0, 52, 104)
                columnHeaderStyle.ForeColor = Color.White
                columnHeaderStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                columnHeaderStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                columnHeaderStyle.WrapMode = DataGridViewTriState.True
                'set into place the previously defined header styles
                .ColumnHeadersDefaultCellStyle = columnHeaderStyle
            End With
            'Set DataGridView textbox Column for Duns
            Dim colDUNS As New DataGridViewTextBoxColumn
            With colDUNS
                .DataPropertyName = "DUNS"
                .Name = "DUNS"
                .Visible = True
                .Width = 78
            End With
            dgvCorporate.Columns.Add(colDUNS)

            'Set DataGridView textbox Column for EmployerID
            Dim colEmployerID As New DataGridViewTextBoxColumn
            With colEmployerID
                .DataPropertyName = "EmployerID"
                '.HeaderText = "MCO Name"
                .Name = "EmployerID"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .Width = 80
            End With
            dgvCorporate.Columns.Add(colEmployerID)


            'Set DataGridView textbox Column for Business Name
            Dim colBusinessName As New DataGridViewTextBoxColumn
            With colBusinessName
                .DataPropertyName = "BusinessName"
                .HeaderText = "Business Name"
                .Name = "BusinessName"
                .Width = 325
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##,##0"
            End With
            dgvCorporate.Columns.Add(colBusinessName)

            'Set DataGridView textbox Column for Address
            Dim colAddress As New DataGridViewTextBoxColumn
            With colAddress
                .DataPropertyName = "Address"
                .HeaderText = "Address"
                .Name = "Address"
                .Width = 290
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvCorporate.Columns.Add(colAddress)

            'Set DataGridView textbox Column for City
            Dim colCity As New DataGridViewTextBoxColumn
            With colCity
                .DataPropertyName = "City"
                .HeaderText = "City"
                .Name = "City"
                .Width = 140
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvCorporate.Columns.Add(colCity)

            'Set DataGridView textbox Column for State
            Dim colState As New DataGridViewTextBoxColumn
            With colState
                .DataPropertyName = "State"
                .HeaderText = "State"
                .Name = "State"
                .Width = 71
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvCorporate.Columns.Add(colState)

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colImportAnalysis As New DataGridViewTextBoxColumn
            With colImportAnalysis
                .DataPropertyName = "ImportAnalysis"
                .HeaderText = "Import Analysis"
                .Name = "ImportAnalysis"
                .Width = 88
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvCorporate.Columns.Add(colImportAnalysis)

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colEmployees As New DataGridViewTextBoxColumn
            With colEmployees
                .DataPropertyName = "Employees"
                .HeaderText = "Employees"
                .Name = "Employees"
                .Width = 70
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "#,###"
            End With
            dgvCorporate.Columns.Add(colEmployees)

            ''don't allow columns to be sorted
            'Dim i As Integer
            'For i = 0 To dgvCorporate.Columns.Count - 1
            '    dgvCorporate.Columns.Item(i).SortMode = DataGridViewColumnSortMode.NotSortable
            '    dgvCorporate.Columns.Item(i).ReadOnly = True
            'Next
        Catch ex As Exception
            Functions.Sendmail(ex.Message, "dgvCorporate_FormatGrid", 0, 0, "MO Entry")
            MsgBox("Mo Entry : dgvCorporate_FormatGrid " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvCorporate_BindData()
        Try
            dgvCorporate.Rows.Clear()
            For i As Integer = 0 To dsCorporateList.Tables(0).Rows.Count - 1
                Me.dgvCorporate.Rows.Add(dsCorporateList.Tables(0).Rows(i).Item(0), dsCorporateList.Tables(0).Rows(i).Item(1), _
                                    dsCorporateList.Tables(0).Rows(i).Item(2), dsCorporateList.Tables(0).Rows(i).Item(3), _
                                    dsCorporateList.Tables(0).Rows(i).Item(4), dsCorporateList.Tables(0).Rows(i).Item(5), _
                                    dsCorporateList.Tables(0).Rows(i).Item(6), dsCorporateList.Tables(0).Rows(i).Item(7))

            Next
        Catch ex As Exception
            Functions.Sendmail(ex.Message, "dgvCorporate_BindData", 0, 0, "MO Entry")
            MsgBox("Mo Entry : dgvCorporate_BindData  : " + ex.Message)
        End Try
    End Sub


    Private Sub dgvSubsidiaryBrowser_FormatGrid()
        'This is a general formatting grid subroutine for the HMO, PPO, HMOMedicare, HMOMedicaid datagrids
        Try
            'set Visual Basic Datagrid Header style to false so we can use our own
            'The key statement required to get the column and row styles to work
            'Visual Header styles must be shut off
            dgvSubsidiaryBrowser.EnableHeadersVisualStyles = False
            'go and set the styles
            With dgvSubsidiaryBrowser
                'the following line is necessary for manual column sizing 
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
                'let the columns size their heights on their own
                .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
                '*** header settings
                'header backcolor, text color, font bold, font, multiline and alignment
                Dim columnHeaderStyle As New DataGridViewCellStyle
                columnHeaderStyle.BackColor = Color.FromArgb(0, 52, 104)
                columnHeaderStyle.ForeColor = Color.White
                columnHeaderStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                columnHeaderStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                columnHeaderStyle.WrapMode = DataGridViewTriState.True
                'set into place the previously defined header styles
                .ColumnHeadersDefaultCellStyle = columnHeaderStyle
            End With
            'Set DataGridView textbox Column for Duns
            Dim colDUNS As New DataGridViewTextBoxColumn
            With colDUNS
                .DataPropertyName = "DUNS"
                .Name = "DUNS"
                .Visible = True
                .Width = 78
            End With
            dgvSubsidiaryBrowser.Columns.Add(colDUNS)

            'Set DataGridView textbox Column for EmployerID
            Dim colEmployerID As New DataGridViewTextBoxColumn
            With colEmployerID
                .DataPropertyName = "EmployerID"
                '.HeaderText = "MCO Name"
                .Name = "EmployerID"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .Width = 79
            End With
            dgvSubsidiaryBrowser.Columns.Add(colEmployerID)


            'Set DataGridView textbox Column for Business Name
            Dim colBusinessName As New DataGridViewTextBoxColumn
            With colBusinessName
                .DataPropertyName = "BusinessName"
                .HeaderText = "Business Name"
                .Name = "BusinessName"
                .Width = 300
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##,##0"
            End With
            dgvSubsidiaryBrowser.Columns.Add(colBusinessName)

            'Set DataGridView textbox Column for Address
            Dim colAddress As New DataGridViewTextBoxColumn
            With colAddress
                .DataPropertyName = "Address"
                .HeaderText = "Address"
                .Name = "Address"
                .Width = 280
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvSubsidiaryBrowser.Columns.Add(colAddress)

            'Set DataGridView textbox Column for City
            Dim colCity As New DataGridViewTextBoxColumn
            With colCity
                .DataPropertyName = "City"
                .HeaderText = "City"
                .Name = "City"
                .Width = 130
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvSubsidiaryBrowser.Columns.Add(colCity)

            'Set DataGridView textbox Column for State
            Dim colState As New DataGridViewTextBoxColumn
            With colState
                .DataPropertyName = "State"
                .HeaderText = "State"
                .Name = "State"
                .Width = 55
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvSubsidiaryBrowser.Columns.Add(colState)

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colImportAnalysis As New DataGridViewTextBoxColumn
            With colImportAnalysis
                .DataPropertyName = "ImportAnalysis"
                .HeaderText = "Import Analysis"
                .Name = "ImportAnalysis"
                .Width = 80
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvSubsidiaryBrowser.Columns.Add(colImportAnalysis)

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colEmployeesHere As New DataGridViewTextBoxColumn
            With colEmployeesHere
                .DataPropertyName = "Employees Here"
                .HeaderText = "Employees Here"
                .Name = "Employees Here"
                .Width = 70
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "#,###"
            End With
            dgvSubsidiaryBrowser.Columns.Add(colEmployeesHere)

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colEmployeesTotal As New DataGridViewTextBoxColumn
            With colEmployeesTotal
                .DataPropertyName = "Employees Total"
                .HeaderText = "Employees Total"
                .Name = "Employees Total"
                .Width = 70
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "#,###"
            End With
            dgvSubsidiaryBrowser.Columns.Add(colEmployeesTotal)

        Catch ex As Exception
            Functions.Sendmail(ex.Message, "dgvSubsidiaryBrowser_FormatGrid", 0, 0, "MO Entry")
            MsgBox("Mo Entry : dgvSubsidiaryBrowser_FormatGrid  : " + ex.Message)
        End Try
    End Sub


    Private Sub dgvSubsidiaryBrowser_BindData()
        Try
            dgvSubsidiaryBrowser.Rows.Clear()

            If dsSubsidiaryList.Tables(0).Rows.Count > 0 Then
                TextBox90.Text = dsSubsidiaryList.Tables(0).Rows(0).Item("ParentDuns")
                For i As Integer = 0 To dsSubsidiaryList.Tables(0).Rows.Count - 1
                    Me.dgvSubsidiaryBrowser.Rows.Add(dsSubsidiaryList.Tables(0).Rows(i).Item(0), dsSubsidiaryList.Tables(0).Rows(i).Item(1), _
                                        dsSubsidiaryList.Tables(0).Rows(i).Item(2), dsSubsidiaryList.Tables(0).Rows(i).Item(3), _
                                        dsSubsidiaryList.Tables(0).Rows(i).Item(4), dsSubsidiaryList.Tables(0).Rows(i).Item(5), _
                                        dsSubsidiaryList.Tables(0).Rows(i).Item(6), dsSubsidiaryList.Tables(0).Rows(i).Item(7), _
                                        dsSubsidiaryList.Tables(0).Rows(i).Item(8))
                Next
            Else
                TextBox90.Text = "None"
            End If
        Catch ex As Exception
            Functions.Sendmail(ex.Message, "dgvSubsidiaryBrowser_BindData", 0, 0, "MO Entry")
            MsgBox("Mo Entry : dgvSubsidiaryBrowser_BindData  : " + ex.Message)
        End Try
    End Sub


#End Region




    Private Sub get_ChangeData()
        Try
            Dim _Change As String
            If radDemotion.Checked Then
                _Change = "Demotion"
            ElseIf radDoubleDemotion.Checked Then
                _Change = "DoubleDemotion"
            ElseIf radPromotion.Checked Then
                _Change = "Promotion"
            ElseIf radAddition.Checked Then
                _Change = "Addition"
            ElseIf radDeletion.Checked Then
                _Change = "Delete"
            End If

            dsChangeRecords = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Change_Data", cmbChangeList.SelectedValue(0), _Change)

            If dsChangeRecords.Tables(0).Rows.Count > 0 Then

                GroupBox2.Text = "Current (" + CStr(cmbChangeList.SelectedValue(0)) + ")"

                TextBox91.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("EIN"))
                TextBox92.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("DOLRecord"))

                TextBox15.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Duns"))
                TextBox16.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Business Name"))
                TextBox71.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Line of Business"))
                TextBox17.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Address"))
                TextBox18.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("City"))
                TextBox19.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("State"))
                TextBox20.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Employees Here"))
                TextBox21.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Employees Total"))
                TextBox79.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("SIC"))

                TextBox22.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("HQ Employees Total"))
                TextBox23.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("HQ Employees Here"))
                TextBox24.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("HQ State"))
                TextBox25.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("HQ City"))
                TextBox26.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("HQ Address"))
                TextBox72.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("HQ Line of Business"))
                TextBox27.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("HQ Business Name"))
                TextBox28.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("HQ Duns"))
                TextBox80.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("HQ SIC"))

                TextBox29.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Domestic Employees Total"))
                TextBox30.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Domestic Employees Here"))
                TextBox31.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Domestic State"))
                TextBox32.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Domestic City"))
                TextBox33.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Domestic Address"))
                TextBox73.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Domestic Line of Business"))
                TextBox34.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Domestic Business Name"))
                TextBox35.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Domestic Duns"))
                TextBox81.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Domestic SIC"))

                TextBox36.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Global Employees Total"))
                TextBox37.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Global Employees Here"))
                TextBox38.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Global State"))
                TextBox39.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Global City"))
                TextBox40.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Global Address"))
                TextBox74.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Global Line of Business"))
                TextBox41.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Global Business Name"))
                TextBox42.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Global Duns"))
                TextBox82.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Global SIC"))

                TextBox64.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Employees Total"))
                TextBox65.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Employees Here"))
                TextBox66.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior State"))
                TextBox67.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior City"))
                TextBox78.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Line of Business"))
                TextBox68.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Address"))
                TextBox69.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Business Name"))
                TextBox70.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Duns"))
                TextBox83.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior SIC"))

                TextBox57.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior HQ Employees Total"))
                TextBox58.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior HQ Employees Here"))
                TextBox59.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior HQ State"))
                TextBox60.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior HQ City"))
                TextBox61.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior HQ Address"))
                TextBox77.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior HQ Line of Business"))
                TextBox62.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior HQ Business Name"))
                TextBox63.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior HQ Duns"))
                TextBox84.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior HQ SIC"))

                TextBox50.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Employees Total"))
                TextBox51.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Employees Here"))
                TextBox52.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Domestic State"))
                TextBox53.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Domestic City"))
                TextBox54.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Address"))
                TextBox76.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Line of Business"))
                TextBox55.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Business Name"))
                TextBox56.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Duns"))
                TextBox85.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Domestic SIC"))

                TextBox43.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Global Employees Total"))
                TextBox44.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Global Employees Here"))
                TextBox45.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Global State"))
                TextBox46.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Global City"))
                TextBox47.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Global Address"))
                TextBox75.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Global Line of Business"))
                TextBox48.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Global Business Name"))
                TextBox49.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Global Duns"))
                TextBox86.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("Prior Global SIC"))
                LinkLabel1.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("URL"))

                If TextBox28.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radDemotion.Checked Or radDoubleDemotion.Checked) Then
                    Label25.BackColor = Color.Red
                Else
                    Label25.BackColor = Color.Transparent
                End If

                If TextBox35.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radDemotion.Checked Or radDoubleDemotion.Checked) Then
                    Label26.BackColor = Color.Red
                Else
                    Label26.BackColor = Color.Transparent
                End If

                If TextBox42.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radDemotion.Checked Or radDoubleDemotion.Checked) Then
                    Label27.BackColor = Color.Red
                Else
                    Label27.BackColor = Color.Transparent
                End If

                If TextBox63.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radPromotion.Checked) Then
                    Label30.BackColor = Color.Green
                Else
                    Label30.BackColor = Color.Transparent
                End If

                If TextBox56.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radPromotion.Checked) Then
                    Label29.BackColor = Color.Green
                Else
                    Label29.BackColor = Color.Transparent
                End If

                If TextBox49.Text = isnull(dsChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radPromotion.Checked) Then
                    Label28.BackColor = Color.Green
                Else
                    Label28.BackColor = Color.Transparent
                End If




            Else
                Label25.BackColor = Color.Transparent
                Label26.BackColor = Color.Transparent
                Label27.BackColor = Color.Transparent
                GroupBox2.Text = "Current"
                TextBox15.Clear()
                TextBox16.Clear()
                TextBox17.Clear()
                TextBox18.Clear()
                TextBox19.Clear()

                TextBox20.Clear()
                TextBox21.Clear()
                TextBox22.Clear()
                TextBox23.Clear()
                TextBox24.Clear()
                TextBox25.Clear()
                TextBox26.Clear()
                TextBox27.Clear()
                TextBox28.Clear()
                TextBox29.Clear()

                TextBox30.Clear()
                TextBox31.Clear()
                TextBox32.Clear()
                TextBox33.Clear()
                TextBox34.Clear()
                TextBox35.Clear()
                TextBox36.Clear()
                TextBox37.Clear()
                TextBox38.Clear()
                TextBox39.Clear()

                TextBox40.Clear()
                TextBox41.Clear()
                TextBox42.Clear()
                TextBox43.Clear()
                TextBox44.Clear()
                TextBox45.Clear()
                TextBox46.Clear()
                TextBox47.Clear()
                TextBox48.Clear()
                TextBox49.Clear()

                TextBox50.Clear()
                TextBox51.Clear()
                TextBox52.Clear()
                TextBox53.Clear()
                TextBox54.Clear()
                TextBox55.Clear()
                TextBox56.Clear()
                TextBox57.Clear()
                TextBox58.Clear()
                TextBox59.Clear()

                TextBox60.Clear()
                TextBox61.Clear()
                TextBox62.Clear()
                TextBox63.Clear()
                TextBox64.Clear()
                TextBox65.Clear()
                TextBox66.Clear()
                TextBox67.Clear()
                TextBox68.Clear()
                TextBox69.Clear()

                TextBox70.Clear()
                TextBox71.Clear()
                TextBox72.Clear()
                TextBox73.Clear()
                TextBox74.Clear()
                TextBox75.Clear()
                TextBox76.Clear()
                TextBox77.Clear()
                TextBox78.Clear()
                TextBox79.Clear()

                TextBox80.Clear()
                TextBox81.Clear()
                TextBox82.Clear()
                TextBox83.Clear()
                TextBox84.Clear()
                TextBox85.Clear()
                TextBox86.Clear()
            End If
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "get_ChangeData ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : get_ChangeData : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub Reprocess_Change_List()
        Dim changeptr As Int16
        Try
            If cmbChangeList.SelectedIndex <> -1 Then
                changeptr = cmbChangeList.SelectedIndex
            End If
            If radDoubleDemotion.Checked Then
                dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_DoubleDemotion_List")
            ElseIf radDemotion.Checked Then
                dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Demotion_List")
            ElseIf radPromotion.Checked Then
                dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Promotion_List")
            ElseIf radAddition.Checked Then
                dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Addition_List")
            ElseIf radDeletion.Checked Then
                dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Delete_List")
            End If
            cmbChangeList.DataSource = dsDemotionList.Tables(0)
            cmbChangeList.DisplayMember = dsDemotionList.Tables(0).Columns("Business Name").ToString
            Label45.Text = CStr(dsDemotionList.Tables(0).Rows.Count - 1)
            'Select first item in list
            If dsDemotionList.Tables(0).Rows.Count > 1 Then
                cmbChangeList.SelectedIndex = changeptr
            Else
                cmbChangeList.SelectedIndex = 0
            End If
            'reset lists on other tabs
            dsCorporateList = SQLHelper.ExecuteDataset(CN, "emp.s_get_Corporate_list")
            dgvCorporate_BindData()
            dsCorpforSubsidiaryList = SQLHelper.ExecuteDataset(CN, "emp.s_Get_Full_List")
            cmbSubsidiaryBrowser.DataSource = dsCorpforSubsidiaryList.Tables(0)
            cmbSubsidiaryBrowser.DisplayMember = dsCorpforSubsidiaryList.Tables(0).Columns("Business Name").ToString

            'fill boxes with results
            get_ChangeData()
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "Reprocess_Change_List ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : Reprocess_Change_List : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try

    End Sub

    Private Sub Reprocess_Sub_List()
        Dim changeptr As Int16
        Try
            If cmbSubList.SelectedIndex <> -1 Then
                changeptr = cmbSubList.SelectedIndex
            End If
            If radSubDemotion.Checked Then
                dsSubDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_SubDemotion_List")
            ElseIf radSubDoubleDemotion.Checked Then
                dsSubDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_SubDoubleDemotion_List")
            ElseIf radSubDelete.Checked And ckbSortAlpha.Checked Then
                dsSubDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_SubDelete_List", 1)
            ElseIf radSubDelete.Checked Then
                dsSubDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_SubDelete_List", 0)
                'ElseIf radAddition.Checked Then
                '    dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Addition_List")
                'ElseIf radDeletion.Checked Then
                '    dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Delete_List")
            End If
            cmbSubList.DataSource = dsSubDemotionList.Tables(0)
            cmbSubList.DisplayMember = dsSubDemotionList.Tables(0).Columns("Business Name").ToString
            Label62.Text = CStr(dsSubDemotionList.Tables(0).Rows.Count - 1)
            'Select first item in list
            If dsSubDemotionList.Tables(0).Rows.Count > 1 Then
                cmbSubList.SelectedIndex = changeptr
            Else
                cmbSubList.SelectedIndex = 0
            End If
    

            'fill boxes with results
            get_SubChangeData()
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "Reprocess_Sub_List ", cmbSubList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : Reprocess_Sub_List : " + cmbSubList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub get_SubChangeData()
        Try
            Label67.BackColor = Color.Transparent
            Label66.BackColor = Color.Transparent
            Label65.BackColor = Color.Transparent


            Dim _Change As String
            If radSubDemotion.Checked Then
                _Change = "Demotion"
                'ElseIf radDoubleDemotion.Checked Then
                '    _Change = "DoubleDemotion"
                'ElseIf radPromotion.Checked Then
                '    _Change = "Promotion"
                'ElseIf radAddition.Checked Then
                '    _Change = "Addition"
            ElseIf radSubDelete.Checked Then
                _Change = "Delete"
            End If

            dsSubChangeRecords = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_subChange_Data", cmbSubList.SelectedValue(0), _Change)

            If dsSubChangeRecords.Tables(0).Rows.Count > 0 Then

                GroupBox7.Text = "Current (" + CStr(cmbSubList.SelectedValue(0)) + ")"

                TextBox101.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("EIN"))
                TextBox100.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("DOLRecord"))

                TextBox137.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Duns"))
                TextBox136.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Business Name"))
                TextBox109.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Line of Business"))
                TextBox135.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Address"))
                TextBox134.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("City"))
                TextBox133.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("State"))
                TextBox132.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Employees Here"))
                TextBox131.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Employees Total"))
                TextBox105.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("SIC"))

                TextBox124.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("HQ Employees Total"))
                TextBox125.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("HQ Employees Here"))
                TextBox126.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("HQ State"))
                TextBox127.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("HQ City"))
                TextBox128.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("HQ Address"))
                TextBox108.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("HQ Line of Business"))
                TextBox129.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("HQ Business Name"))
                TextBox130.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("HQ Duns"))
                TextBox104.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("HQ SIC"))

                TextBox123.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Domestic Duns"))
                TextBox122.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Domestic Business Name"))
                TextBox107.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Domestic Line of Business"))
                TextBox103.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Domestic SIC"))
                TextBox121.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Domestic Address"))
                TextBox120.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Domestic City"))
                TextBox119.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Domestic State"))
                TextBox118.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Domestic Employees Here"))
                TextBox117.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Domestic Employees Total"))

                TextBox110.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Global Employees Total"))
                TextBox111.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Global Employees Here"))
                TextBox112.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Global State"))
                TextBox113.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Global City"))
                TextBox114.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Global Address"))
                TextBox106.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Global Line of Business"))
                TextBox115.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Global Business Name"))
                TextBox116.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Global Duns"))
                TextBox102.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Global SIC"))

                TextBox167.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Employees Total"))
                TextBox168.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Employees Here"))
                TextBox169.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior State"))
                TextBox170.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior City"))
                TextBox145.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Line of Business"))
                TextBox171.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Address"))
                TextBox172.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Business Name"))
                TextBox173.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Duns"))
                TextBox141.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior SIC"))

                TextBox160.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior HQ Employees Total"))
                TextBox161.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior HQ Employees Here"))
                TextBox162.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior HQ State"))
                TextBox163.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior HQ City"))
                TextBox164.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior HQ Address"))
                TextBox144.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior HQ Line of Business"))
                TextBox165.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior HQ Business Name"))
                TextBox166.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior HQ Duns"))
                TextBox140.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior HQ SIC"))

                TextBox153.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Employees Total"))
                TextBox154.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Employees Here"))
                TextBox155.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Domestic State"))
                TextBox156.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Domestic City"))
                TextBox157.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Address"))
                TextBox143.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Line of Business"))
                TextBox158.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Business Name"))
                TextBox159.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Domestic Duns"))
                TextBox139.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Domestic SIC"))

                TextBox146.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Global Employees Total"))
                TextBox147.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Global Employees Here"))
                TextBox148.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Global State"))
                TextBox149.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Global City"))
                TextBox150.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Global Address"))
                TextBox142.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Global Line of Business"))
                TextBox151.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Global Business Name"))
                TextBox152.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Global Duns"))
                TextBox138.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Prior Global SIC"))
                'LinkLabel1.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("URL"))
                TextBox174.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("Existing Parent"))

                If TextBox130.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radSubDemotion.Checked) Then 'Or radDoubleDemotion.Checked
                    Label67.BackColor = Color.Red
                End If

                If TextBox123.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radSubDemotion.Checked) Then ' Or radDoubleDemotion.Checked
                    Label66.BackColor = Color.Red
                End If

                If TextBox116.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radSubDemotion.Checked) Then 'Or radDoubleDemotion.Checked
                    Label65.BackColor = Color.Red
                End If

                'If TextBox63.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radPromotion.Checked) Then
                '    Label30.BackColor = Color.Green
                'Else
                '    Label30.BackColor = Color.Transparent
                'End If

                'If TextBox56.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radPromotion.Checked) Then
                '    Label29.BackColor = Color.Green
                'Else
                '    Label29.BackColor = Color.Transparent
                'End If

                'If TextBox49.Text = isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("ParentDuns")) And (radPromotion.Checked) Then
                '    Label28.BackColor = Color.Green
                'Else
                '    Label28.BackColor = Color.Transparent
                'End If




            Else
                'Label25.BackColor = Color.Transparent
                'Label26.BackColor = Color.Transparent
                'Label27.BackColor = Color.Transparent
                'GroupBox2.Text = "Current"
                TextBox129.Clear()
                TextBox108.Clear()
                TextBox104.Clear()
                TextBox128.Clear()
                TextBox127.Clear()

                TextBox137.Clear()
                TextBox136.Clear()
                TextBox109.Clear()
                TextBox105.Clear()
                TextBox135.Clear()
                TextBox134.Clear()
                TextBox133.Clear()
                TextBox132.Clear()
                TextBox131.Clear()
                TextBox130.Clear()

                TextBox126.Clear()
                TextBox125.Clear()
                TextBox124.Clear()
                TextBox123.Clear()
                TextBox122.Clear()
                TextBox107.Clear()
                TextBox103.Clear()
                TextBox121.Clear()
                TextBox120.Clear()
                TextBox119.Clear()

                TextBox118.Clear()
                TextBox117.Clear()
                TextBox116.Clear()
                TextBox115.Clear()
                TextBox106.Clear()
                TextBox102.Clear()
                TextBox114.Clear()
                TextBox113.Clear()
                TextBox112.Clear()
                TextBox111.Clear()

                TextBox110.Clear()
                TextBox173.Clear()
                TextBox172.Clear()
                TextBox145.Clear()
                TextBox141.Clear()
                TextBox171.Clear()
                TextBox170.Clear()
                TextBox169.Clear()
                TextBox168.Clear()
                TextBox167.Clear()

                TextBox166.Clear()
                TextBox165.Clear()
                TextBox144.Clear()
                TextBox140.Clear()
                TextBox164.Clear()
                TextBox163.Clear()
                TextBox162.Clear()
                TextBox161.Clear()
                TextBox160.Clear()
                TextBox159.Clear()

                TextBox158.Clear()
                TextBox143.Clear()
                TextBox139.Clear()
                TextBox157.Clear()
                TextBox156.Clear()
                TextBox155.Clear()
                TextBox154.Clear()
                TextBox153.Clear()
                TextBox152.Clear()
                TextBox79.Clear()

                TextBox80.Clear()
                TextBox151.Clear()
                TextBox142.Clear()
                TextBox138.Clear()
                TextBox150.Clear()
                TextBox149.Clear()
                TextBox148.Clear()

                TextBox147.Clear()
                TextBox146.Clear()
            End If
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "get_ChangeData ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : get_ChangeData : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub radDoubleDemotion_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles radDoubleDemotion.CheckedChanged
        If radDoubleDemotion.Checked And Not bInitial Then
            Label44.Visible = True
            Label46.Visible = False
            Label47.Visible = False
            ToolTip1.SetToolTip(btnAccepttheChange, "Accepting the demotion Moves the Company from the Corporate list to the Subsidiary/branch list")
            ToolTip1.SetToolTip(btnRejectChange, "Rejecting the demotion resets the ParentID for this company to itself and marks it to be left alone.")
            Try
                ' change combobox to be filled with Demotion list.
                bInitial = True
                Reprocess_Change_List()
                bInitial = False
            Catch ex As Exception
                bInitial = False
                'Functions.Sendmail(ex.Message, "radDoubleDemotion_CheckedChanged ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
                MsgBox("Employer Maintenance : radDoubleDemotion_CheckedChanged : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
            End Try

        End If
    End Sub

    Private Sub radPromotion_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles radPromotion.CheckedChanged
        If radPromotion.Checked And Not bInitial Then
            Label44.Visible = False
            Label46.Visible = True
            Label47.Visible = False

            ToolTip1.SetToolTip(btnAccepttheChange, "Accepting the Promotion Delete the Company from the Subsidiary/branch list.  It already exisits in the Corporate list. " _
                                                    + "All subsidiaries already point to this Company")
            ToolTip1.SetToolTip(btnRejectChange, "Rejecting the Promotion leaves this company in the subsidiary table and clears it from the Corporate")
            Try
                ' change combobox to be filled with Demotion list.
                bInitial = True
                Reprocess_Change_List()
                bInitial = False
            Catch ex As Exception
                bInitial = False
                'Functions.Sendmail(ex.Message, "radDoubleDemotion_CheckedChanged ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
                MsgBox("Employer Maintenance : radDoubleDemotion_CheckedChanged : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
            End Try

        End If
    End Sub

    Private Sub radAddition_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles radAddition.CheckedChanged
        If radAddition.Checked And Not bInitial Then
            Label44.Visible = False
            Label46.Visible = False
            Label47.Visible = True

            ToolTip1.SetToolTip(btnAccepttheChange, "Accepting the Promotion Delete the Company from the Subsidiary/branch list.  It already exisits in the Corporate list. " _
                                                    + "All subsidiaries already point to this Company")
            ToolTip1.SetToolTip(btnRejectChange, "Rejecting the Promotion leaves this company in the subsidiary table and clears it from the Corporate")
            Try
                ' change combobox to be filled with Demotion list.
                bInitial = True
                Reprocess_Change_List()
                bInitial = False
            Catch ex As Exception
                bInitial = False
                'Functions.Sendmail(ex.Message, "radDoubleDemotion_CheckedChanged ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
                MsgBox("Employer Maintenance : radDoubleDemotion_CheckedChanged : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
            End Try

        End If
    End Sub

    Private Sub radDeletion_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles radDeletion.CheckedChanged
        If radDeletion.Checked And Not bInitial Then
            Label44.Visible = False
            Label46.Visible = False
            Label47.Visible = False

            ToolTip1.SetToolTip(btnAccepttheChange, "Accepting the Deletion Deletes the Corporation from the list and all subsidiaries that point to it")
            ToolTip1.SetToolTip(btnRejectChange, "Rejecting the Deletion leaves this company in the subsidiary table and clears it from the Corporate delete list")
            Try
                ' change combobox to be filled with Demotion list.
                bInitial = True
                Reprocess_Change_List()
                bInitial = False
            Catch ex As Exception
                bInitial = False
                'Functions.Sendmail(ex.Message, "radDeletion_CheckedChanged ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
                MsgBox("Employer Maintenance : radDeletion_CheckedChanged : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
            End Try

        End If
    End Sub

    Private Sub cmbSubsidiaryBrowser_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cmbSubsidiaryBrowser.SelectedIndexChanged
        Try
            If Not bInitial And cmbSubsidiaryBrowser.SelectedIndex > 0 Then
                bInitial = True
                dsSubsidiaryList = SQLHelper.ExecuteDataset(CN, "emp.s_get_Subsidiarylist", cmbSubsidiaryBrowser.SelectedValue(0))
                dgvSubsidiaryBrowser_BindData()
                bInitial = False
            End If
        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "cmbSubsidiaryBrowser_SelectedIndexChanged ", cmbSubsidiaryBrowser.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : cmbSubsidiaryBrowser_SelectedIndexChanged : " + cmbSubsidiaryBrowser.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnFilter_Click(sender As System.Object, e As System.EventArgs) Handles btnFilter.Click
        Try

            If Not bInitial Then
                dsCorporateList = SQLHelper.ExecuteDataset(CN, "emp.s_get_Filtered_list", _
                                                          IIf(Len(TextBox87.Text) = 0, DBNull.Value, "%" + TextBox87.Text + "%"), _
                                                          IIf(Len(TextBox88.Text) = 0, DBNull.Value, "%" + TextBox88.Text + "%"), _
                                                          IIf(Len(TextBox89.Text) = 0, DBNull.Value, TextBox89.Text))
                dgvCorporate_BindData()
            End If
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "btnFilter_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnFilter_Click : " + ex.Message)
        End Try
    End Sub


 
    Private Sub btnOrphanedHolding_Click(sender As System.Object, e As System.EventArgs) Handles btnOrphanedHolding.Click
        Try
            If Not bInitial Then
                btnOrphanedHolding.Visible = False
                SQLHelper.ExecuteScalar(CN, "EMP.s_Delete_OrphanedHolding")

                'reset lists on other tabs
                dsCorporateList = SQLHelper.ExecuteDataset(CN, "emp.s_get_Corporate_list")
                dgvCorporate_BindData()
                dsCorpforSubsidiaryList = SQLHelper.ExecuteDataset(CN, "emp.s_Get_Full_List")
                cmbSubsidiaryBrowser.DataSource = dsCorpforSubsidiaryList.Tables(0)
                cmbSubsidiaryBrowser.DisplayMember = dsCorpforSubsidiaryList.Tables(0).Columns("Business Name").ToString

  
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Delete_OrphanedHolding")
                    sw.WriteLine("Go")
                End Using
                btnOrphanedHolding.Visible = True
            End If
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "btnOrphanedHolding_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnOrphanedHolding_Click : " + ex.Message)
        End Try
    End Sub

   
    Private Sub radSubDemotion_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles radSubDemotion.CheckedChanged
        If radSubDemotion.Checked And Not bInitial Then
            btnAcceptSubChange.Visible = True
            btnRejectSubChange.Visible = True
            Label90.Visible = True
            ToolTip1.SetToolTip(btnAcceptSubChange, "Accept the analysis for a demotion (from corporate to second tier)")
            ToolTip1.SetToolTip(btnRejectSubChange, "Reject the analysis for a demotion (from corporate to second tier)")
            Try
                ' change combobox to be filled with Demotion list.
                bInitial = True
                Reprocess_Sub_List()
                bInitial = False
            Catch ex As Exception
                bInitial = False
                'Functions.Sendmail(ex.Message, "radSubDemotion_CheckedChanged ", cmbSubList.SelectedValue(1), 0, "Employer Maintenance")
                MsgBox("Employer Maintenance : radSubDemotion_CheckedChanged : " + cmbSubList.SelectedValue(1) + " : " + ex.Message)
            End Try

        End If
    End Sub

    Private Sub cmbSubList_SelectedIndexChanged(sender As System.Object, e As System.EventArgs) Handles cmbSubList.SelectedIndexChanged
        Try
            If Not bInitial And cmbSubList.SelectedIndex > 0 Then
                bInitial = True
                get_SubChangeData()
                bInitial = False
            End If
        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "cmbChangeList_SelectedIndexChanged ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : cmbChangeList_SelectedIndexChanged : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnAcceptSubChange_Click(sender As System.Object, e As System.EventArgs) Handles btnAcceptSubChange.Click
        Dim iresult As Integer
        Dim indexpointer As Int16
        Try
            bInitial = True
            indexpointer = cmbSubList.SelectedIndex



            If radSubDemotion.Checked Or radSubDoubleDemotion.Checked Then
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_SubDemotion", cmbSubList.SelectedValue(0), strCurrentUser)
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_SubDemotion " + CStr(cmbSubList.SelectedValue(0)) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using
                'ElseIf radPromotion.Checked Then
                '    iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_Promotion", cmbChangeList.SelectedValue(0), strCurrentUser)
                '    'objWriter.WriteLine("EMP.s_Accept_Promotion " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                '    Using sw As StreamWriter = File.AppendText(path)
                '        sw.WriteLine("EMP.s_Accept_Promotion " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                '        sw.WriteLine("Go")
                '    End Using
            ElseIf radSubDelete.Checked Then
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_SubDelete", cmbSubList.SelectedValue(0), strCurrentUser)
                'objWriter.WriteLine("EMP.s_Accept_Delete " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_SubDelete " + CStr(cmbSubList.SelectedValue(0)) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using
                'ElseIf radAddition.Checked Then
                '    iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_Addition", cmbChangeList.SelectedValue(0), strCurrentUser)
                '    'objWriter.WriteLine("EMP.s_Accept_Delete " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                '    Using sw As StreamWriter = File.AppendText(path)
                '        sw.WriteLine("EMP.s_Accept_Addition " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
                '        sw.WriteLine("Go")
                '    End Using
            End If

            If iresult = 0 Then
                Reprocess_Sub_List()
            End If

            bInitial = False

        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnAccepttheChange_Click ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAccepttheChange_Click : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnRejectSubChange_Click(sender As System.Object, e As System.EventArgs) Handles btnRejectSubChange.Click
        Dim iresult As Integer
        Try
            bInitial = True
            If radSubDemotion.Checked Then
                MsgBox("There currently is no solution for this.  Please contact the DBA/Developer for a solution")
            ElseIf radSubDoubleDemotion.Checked Then
                MsgBox("There currently is no solution for this.  Please contact the DBA/Developer for a solution")
            ElseIf radSubDelete.Checked Then
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Reject_SubDelete", cmbSubList.SelectedValue(0), strCurrentUser)
                If iresult = 0 Then
                    Using sw As StreamWriter = File.AppendText(path)
                        sw.WriteLine("EMP.s_Reject_SubDelete " + CStr(cmbSubList.SelectedValue(0)) + ", " + strCurrentUser)
                        sw.WriteLine("Go")
                    End Using
                End If
            End If
            'If radAddition.Checked Then
            '    iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Reject_Addition", cmbChangeList.SelectedValue(0), strCurrentUser)
            '    If iresult = 0 Then
            '        Using sw As StreamWriter = File.AppendText(path)
            '            sw.WriteLine("EMP.s_Reject_Addition " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
            '            sw.WriteLine("Go")
            '        End Using
            '    End If
            'Else
            '    iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Reject_Demotion", cmbChangeList.SelectedValue(0), strCurrentUser)
            '    If iresult = 0 Then
            '        Using sw As StreamWriter = File.AppendText(path)
            '            sw.WriteLine("EMP.s_Reject_Demotion " + CStr(cmbChangeList.SelectedValue(0)) + ", " + strCurrentUser)
            '            sw.WriteLine("Go")
            '        End Using
            '    End If
            'End If

            Reprocess_Sub_List()
            bInitial = False

        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnRejectChange_Click ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnRejectChange_Click : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnRefresh_Click(sender As System.Object, e As System.EventArgs) Handles btnRefresh.Click
        dsStats = SQLHelper.ExecuteDataset(CN, "emp.s_Get_Stats")
        fillstats()
    End Sub


    Private Sub radSubPromotion_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles radSubPromotion.CheckedChanged
        If radSubPromotion.Checked And Not bInitial Then
            btnAcceptSubChange.Visible = False
            btnRejectSubChange.Visible = False
            MsgBox("There currently is no code to handle this.  If the Stats page shows Subsidiary Promotion, then contact the DBA/Developer for a solution")
        End If

    End Sub

    Private Sub radSubAddition_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles radSubAddition.CheckedChanged
        If radSubAddition.Checked And Not bInitial Then
            btnAcceptSubChange.Visible = False
            btnRejectSubChange.Visible = False
            MsgBox("There currently is no code to handle this.  If the Stats page shows Subsidiary Addition and you really want to evalute these, then contact the DBA/Developer for a solution")
        End If
     End Sub

    Private Sub radSubDelete_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles radSubDelete.CheckedChanged
        If radSubDelete.Checked And Not bInitial Then
            'GroupBox7.Visible = False
            'GroupBox9.Visible = False
            Label90.Visible = False
            'Label89.Visible = False
            btnMatchSubDelete.Visible = True
            MsgBox("There tend to be thousands of these. You have the Match Deletes button and you have a partial list to work with.")
            btnAcceptSubChange.Visible = True
            btnRejectSubChange.Visible = True
            btnRejectAllChanges.Visible = True
            btnAcceptAllChanges.Visible = True
            ckbExpandedList.Visible = True
            LinkLabel3.Visible = True
            ckbSortAlpha.Visible = True
            Label91.Visible = True
            TextBox174.Visible = True
            ToolTip1.SetToolTip(btnAcceptSubChange, "Accept the analysis for a Deletion")
            ToolTip1.SetToolTip(btnRejectSubChange, "Reject the analysis for a Deletion")
            Try
                ' change combobox to be filled with Demotion list.
                bInitial = True
                Reprocess_Sub_List()
                bInitial = False
            Catch ex As Exception
                bInitial = False
                'Functions.Sendmail(ex.Message, "radSubDemotion_CheckedChanged ", cmbSubList.SelectedValue(1), 0, "Employer Maintenance")
                MsgBox("Employer Maintenance : radSubDemotion_CheckedChanged : " + cmbSubList.SelectedValue(1) + " : " + ex.Message)
            End Try
        ElseIf Not radSubDelete.Checked And Not bInitial Then
            'GroupBox7.Visible = True
            'GroupBox9.Visible = True
            Label90.Visible = True
            Label91.Visible = False
            TextBox174.Visible = False
            btnRejectAllChanges.Visible = False
            btnAcceptAllChanges.Visible = False
            ckbExpandedList.Visible = False
            LinkLabel3.Visible = False
            ckbSortAlpha.Visible = False
            'Label89.Visible = True
            'btnAcceptSubChange.Visible = True
            'btnRejectSubChange.Visible = True
            btnMatchSubDelete.Visible = False
        End If
     End Sub



    Private Sub radSubDoubleDemotion_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles radSubDoubleDemotion.CheckedChanged
        If radSubDoubleDemotion.Checked And Not bInitial Then
            btnAcceptSubChange.Visible = True
            btnRejectSubChange.Visible = True
            Label90.Visible = True
            ToolTip1.SetToolTip(btnAcceptSubChange, "Accept the analysis for a double demotion (from corporate to third tier or lower)")
            ToolTip1.SetToolTip(btnRejectSubChange, "Reject the analysis for a double demotion (from corporate to third tier or lower)")
            Try
                ' change combobox to be filled with Demotion list.
                bInitial = True
                Reprocess_Sub_List()
                bInitial = False
            Catch ex As Exception
                bInitial = False
                'Functions.Sendmail(ex.Message, "radSubDemotion_CheckedChanged ", cmbSubList.SelectedValue(1), 0, "Employer Maintenance")
                MsgBox("Employer Maintenance : radSubDemotion_CheckedChanged : " + cmbSubList.SelectedValue(1) + " : " + ex.Message)
            End Try

        End If
    End Sub

    Private Sub btnMatchSubDelete_Click(sender As System.Object, e As System.EventArgs) Handles btnMatchSubDelete.Click

        Try
            SQLHelper.ExecuteDataset(CN, "emp.s_MatchDeletionstoAdditions")
            MsgBox("Records removed.  Please review in the stats tab to see the effect")
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "btnMatchSubDelete_Click ", cmbSubList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnMatchSubDelete_Click : " + cmbSubList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnRejectAllChanges_Click(sender As System.Object, e As System.EventArgs) Handles btnRejectAllChanges.Click
        Dim iresult As Integer
        Try
            bInitial = True

            iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Reject_AllChanges", isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("parentid")), strCurrentUser)
            If iresult = 0 Then
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Reject_AllChanges " + CStr(isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("parentid"))) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using
            End If

            Reprocess_Sub_List()
            bInitial = False

        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnRejectAllChanges_Click ", cmbSubList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnRejectAllChanges_Click : " + cmbSubList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnAcceptAllChanges_Click(sender As System.Object, e As System.EventArgs) Handles btnAcceptAllChanges.Click
        Dim iresult As Integer
        Try
            bInitial = True

            iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_AllChanges", isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("parentid")), strCurrentUser)
            If iresult = 0 Then
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_AllChanges " + CStr(isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("parentid"))) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using
            End If

            Reprocess_Sub_List()
            bInitial = False

        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnAcceptAllChanges_Click ", cmbSubList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAcceptAllChanges_Click : " + cmbSubList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub ckbSortAlpha_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles ckbSortAlpha.CheckedChanged
        Reprocess_Sub_List()
    End Sub

    Private Sub LinkLabel3_LinkClicked(sender As System.Object, e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles LinkLabel3.LinkClicked
        System.Diagnostics.Process.Start("http://myreports/DRG/Pages/Report.aspx?ItemPath=%2fProd%2fEV+Validation%2fCorporations+that+ALL+Subsidiaries+were+marked+for+Delete+(not+found+in+the+incoming+list)")
    End Sub

    Private Sub ckbExpandedList_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles ckbExpandedList.CheckedChanged

    End Sub
End Class
