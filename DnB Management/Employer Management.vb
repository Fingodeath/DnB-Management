Option Explicit On
Imports System.Net.Mail
Imports System.IO
Imports GlobalLibrary
Imports System.Data.SqlClient
Imports System.ComponentModel

Public Class Form1


#Region "Declarations for Global Library dll"
    '*** the following lines are for the developer to perform troubleshooting/testing on HLSV4
    '*** this will override the CN settings in the Global Library dll
    'Private Development As New GlobalLibrary.Development("Testing")
    'Private Development As New GlobalLibrary.Development("DataRepository")

    Private DBParameters As New GlobalLibrary.DBParameters(Enums.DatabaseMode.Production, "NASPROSQL1")
    Private Functions As New GlobalLibrary.Functions
    Private SQLHelper As New GlobalLibrary.SqlHelper
    Private usrApplicationManagment As New GlobalLibrary.ApplicationAccess.DRUser()
    Private CN As String = DBParameters.CN
    Private cnSQL As SqlClient.SqlConnection = New SqlClient.SqlConnection(DBParameters.CN)
#End Region


    Private filePath As String
    Private bInitial As Boolean = True
    Private strCurrentUser As String
    Public Userid As String
    Private boolReadOnly As Boolean

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

    Private dsDOLF5500 As DataSet
    Private dsDOLF5500Deletes As DataSet
    Private dsDOLCleanStats As DataSet
    Private dsDOLScheduleA As DataSet
    Private dsDOLScheduleA_Deletes As DataSet
    Private dsDOLScheduleC As DataSet
    Private dsDOLScheduleC_Deletes As DataSet
    Private dsDuns_Dln As DataSet
    Private dsPBMs As DataSet
    Private dsPBMList As DataSet
    Dim path As String = "\\nasprosql1\Dunn & Bradstreet\DnBFlow.sql"
    Dim b5KLimit As Boolean = True


    Private Sub Form1_Load(sender As Object, e As System.EventArgs) Handles Me.Load

        Userid = ApplicationAccess.DRUser.UserID

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
            If Not ApplicationAccess.DRUser.HasAccess("Employer Management", "Access") Then
                Functions.Sendmail("Entry Denial", "Form Load", ApplicationAccess.DRUser.UserID, "", "Employer Management")
                MsgBox("You do not have permission to access this application")
                Me.Close()
                Exit Sub
            End If
            strCurrentUser = ApplicationAccess.DRUser.UserID
            Me.Text = "Employer Management Tool      " _
                    & "           Welcome " _
                    & ApplicationAccess.DRUser.UserProperties("FirstName") + " " _
                    & ApplicationAccess.DRUser.UserProperties("LastName")

            ToolStripStatusLabel1.Text = "Database Server Name:  " + DBParameters.ServerName
            ToolStripStatusLabel2.Text = "     Database:  " + DBParameters.databaseName
            ToolStripStatusLabel3.Text = "     Database Mode:  " + DBParameters.DatabaseMode.ToString
            ToolStripStatusLabel4.Text = "          Version: 1.0.9.14"

            If Not ApplicationAccess.DRUser.HasAccess("Employer Management", "Update") Then
                boolReadOnly = True
            Else
                boolReadOnly = False
            End If

            Functions.ResetBackColorOnView(Me, Me.DBParameters.Backcolor)

            dsCorporateList = SQLHelper.ExecuteDataset(CN, "emp.s_get_Corporate_list")
            dgvCorporate_FormatGrid()
            dgvCorporate_BindData()
            dgvSubsidiaryBrowser_FormatGrid()

            dsCorpforSubsidiaryList = SQLHelper.ExecuteDataset(CN, "emp.s_Get_Full_List")
            cmbSubsidiaryBrowser.DataSource = dsCorpforSubsidiaryList.Tables(0)
            cmbSubsidiaryBrowser.DisplayMember = dsCorpforSubsidiaryList.Tables(0).Columns("Business Name").ToString

            dsStats = SQLHelper.ExecuteDataset(CN, "emp.s_Get_Stats")
            fillstats()

            dsDOLF5500 = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_5500", b5KLimit)
            dgvClean5500_FormatGrid()
            dgvClean5500_BindData()

            dsDOLF5500Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_5500", b5KLimit)
            dgvDirty5500_FormatGrid()
            dgvDirty5500_BindData()

            dsDOLScheduleA = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_A")
            dgvSched_A_FormatGrid()
            dgvSched_A_BindData()

            dsDOLScheduleA_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_A")
            dgv_Sched_A_Drop_FormatGrid()
            dgv_Sched_A_Drop_BindData()

            dsDOLScheduleC = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_C")
            dgvSched_C_FormatGrid()
            dgvSched_C_BindData()

            dsDOLScheduleC_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_C")
            dgv_Sched_C_Drop_FormatGrid()
            dgv_Sched_C_Drop_BindData()

            dsDuns_Dln = SQLHelper.ExecuteDataset(CN, "emp.s_get_Duns_Dln")
            dgvDuns_Dln_FormatGrid()
            dgvDuns_Dln_BindData()

            dsPBMs = SQLHelper.ExecuteDataset(CN, "emp.s_get_PBMs")
            dgvPBMs_FormatGrid()
            dgvPBMs_BindData()

            dsPBMList = SQLHelper.ExecuteDataset(CN, "MOV.s_GetExternalPBMCNames")
            cmbPBM.DataSource = dsPBMList.Tables(0)
            cmbPBM.DisplayMember = dsPBMList.Tables(0).Columns("PBMCName").ToString

            get_DOL_CleanStats()

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
            ToolTip1.SetToolTip(btnAcceptAllRemainingDeletes, "Caution, this clears all the subsidiaries that are marked for deletion.  The amount of records affected is listed above")
            ToolTip1.SetToolTip(btnProviderEIN, "Eliminates those Schedule Cs with no Provider and no Provider EIN")
            ToolTip1.SetToolTip(btnRemove5500s, "This removes all the 5500s that were marked for deletion.  It also removes any associated Duns to DLN, Schedule A and Schedule C records")
            ToolTip1.SetToolTip(btnVerifyWelfareBenefit, "Marks those records where Welfare Benfit Code not in 4A or 4Q")
            ToolTip1.SetToolTip(btnFix_FundingGenAsset, "Sets funding Gen asset to 1 where it is not and Wlfr bnft stop loss ind = 1")
            ToolTip1.SetToolTip(btnExperience, "recalculates the Experience and Non Experience RatedPremiumsPMPM where the stored number is incorrect.")
            ToolTip1.SetToolTip(btnEliminateNonAdmin, "Marks non-admin records ('employee', 'Atorney','Auditior', etc.) for deletion")
            ToolTip1.SetToolTip(txtEIN, "EIN must be 9 characters in length with leading zeros.  Valid characters are 0-9.")

            bInitial = False
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "Form Load", 0, 0, "DnB Management")
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
            TextBox175.Text = isnull(dsStats.Tables(0).Rows(0).Item("Corporate Orphaned Demotion"))

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


#End Region

#Region "Radiobuttons"

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
            btnAcceptAllRemainingDeletes.Visible = True
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
            btnAcceptAllRemainingDeletes.Visible = False
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
            If Not boolReadOnly Then
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
            End If
    

        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnAccepttheChange_Click ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAccepttheChange_Click : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnRejectChange_Click(sender As System.Object, e As System.EventArgs) Handles btnRejectChange.Click
        Dim iresult As Integer
        Try
            If Not boolReadOnly Then
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
            End If


        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnRejectChange_Click ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnRejectChange_Click : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
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

    Private Sub btnAcceptSubChange_Click(sender As System.Object, e As System.EventArgs) Handles btnAcceptSubChange.Click
        Dim iresult As Integer
        Dim indexpointer As Int16
        Try
            If Not boolReadOnly Then
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
            End If

        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnAccepttheChange_Click ", cmbChangeList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAccepttheChange_Click : " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnRejectSubChange_Click(sender As System.Object, e As System.EventArgs) Handles btnRejectSubChange.Click
        Dim iresult As Integer
        Try
            If Not boolReadOnly Then
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
            End If

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

    Private Sub btnMatchSubDelete_Click(sender As System.Object, e As System.EventArgs) Handles btnMatchSubDelete.Click

        Try
            If Not boolReadOnly Then
                SQLHelper.ExecuteDataset(CN, "emp.s_MatchDeletionstoAdditions")
                MsgBox("Records removed.  Please review in the stats tab to see the effect")
            End If
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "btnMatchSubDelete_Click ", cmbSubList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnMatchSubDelete_Click : " + cmbSubList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnRejectAllChanges_Click(sender As System.Object, e As System.EventArgs) Handles btnRejectAllChanges.Click
        Dim iresult As Integer
        Try
            If Not boolReadOnly Then
                bInitial = True

                If isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("parentid")) = "" Then
                    MsgBox("This subsidiary is missing a Parent")
                Else
                    iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Reject_AllChanges", isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("parentid")), strCurrentUser)
                    If iresult = 0 Then
                        Using sw As StreamWriter = File.AppendText(path)
                            sw.WriteLine("EMP.s_Reject_AllChanges " + CStr(isnull(dsSubChangeRecords.Tables(0).Rows(0).Item("parentid"))) + ", " + strCurrentUser)
                            sw.WriteLine("Go")
                        End Using
                    End If
                End If


                Reprocess_Sub_List()
                bInitial = False

            End If

        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnRejectAllChanges_Click ", cmbSubList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnRejectAllChanges_Click : " + cmbSubList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnAcceptAllChanges_Click(sender As System.Object, e As System.EventArgs) Handles btnAcceptAllChanges.Click
        Dim iresult As Integer
        Try
            If Not boolReadOnly Then
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
            End If
        Catch ex As Exception
            bInitial = False
            'Functions.Sendmail(ex.Message, "btnAcceptAllChanges_Click ", cmbSubList.SelectedValue(1), 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAcceptAllChanges_Click : " + cmbSubList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub btnAcceptAllRemainingDeletes_Click(sender As System.Object, e As System.EventArgs) Handles btnAcceptAllRemainingDeletes.Click
        Dim iresult As Integer
        Try
            If Not boolReadOnly Then

                bInitial = True

                iresult = MsgBox("This will delete all of the " + TextBox11.Text + " records marked for deletion.  Are you sure you want to do this?  If you select 'OK' then you must wait for the mail confirmation.", MsgBoxStyle.YesNo)
                If iresult = 6 Then
                    Me.Cursor = Cursors.AppStarting

                    GlobalLibrary.SqlHelper.ExecuteScalar(CN, "dbo.s_UpdateETLParameter", "Employer Management", "_User", strCurrentUser)

                    RUN_DTSX_Package("Delete Remaining Employers")

                    Me.Cursor = Cursors.Default
                End If

                'Reprocess_Sub_List()
                bInitial = False
            End If

        Catch ex As Exception
            bInitial = False
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnAcceptAllRemainingDeletes_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAcceptAllRemainingDeletes_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnWelfareBNFTCode_Click(sender As System.Object, e As System.EventArgs) Handles btnWelfareBNFTCode.Click
        Dim iresult As Integer
        Try
            If Not boolReadOnly Then
                bInitial = True
                Me.Cursor = Cursors.AppStarting

                Using conn As New SqlClient.SqlConnection(CN)
                    conn.Open()
                    Using cm As New SqlClient.SqlCommand("emp.s_RemoveWelfareBenefit", conn)
                        cm.CommandType = CommandType.StoredProcedure
                        cm.CommandTimeout = 3000
                        cm.Parameters.Add("@user", SqlDbType.VarChar)
                        cm.Parameters("@user").Value = strCurrentUser

                        cm.ExecuteNonQuery()
                    End Using
                End Using


                If iresult = 0 Then
                    dsDOLF5500 = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_5500", b5KLimit)
                    dgvClean5500_BindData()

                    dsDOLF5500Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_5500", b5KLimit)
                    dgvDirty5500_BindData()

                    get_DOL_CleanStats()
                End If

                Me.Cursor = Cursors.Default
                bInitial = False
            End If

        Catch ex As Exception
            bInitial = False
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnWelfareBNFTCode_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnWelfareBNFTCode_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnTaxPrepDate_Click(sender As System.Object, e As System.EventArgs) Handles btnTaxPrepDate.Click
        Dim iresult As Integer
        Try
            If Not boolReadOnly Then
                bInitial = True
                Me.Cursor = Cursors.AppStarting

                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_RemovebyTaxDate", TextBox176.Text, strCurrentUser)

                If iresult = 0 Then
                    dsDOLF5500 = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_5500", b5KLimit)
                    dgvClean5500_BindData()

                    dsDOLF5500Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_5500", b5KLimit)
                    dgvDirty5500_BindData()

                    get_DOL_CleanStats()
                End If

                Me.Cursor = Cursors.Default
                bInitial = False
            End If

        Catch ex As Exception
            bInitial = False
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnTaxPrepDate_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnTaxPrepDate_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnInjury_Click(sender As System.Object, e As System.EventArgs) Handles btnInjury.Click
        Dim iresult As Integer
        Try
            If Not boolReadOnly Then
                bInitial = True
                Me.Cursor = Cursors.AppStarting

                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_RemoveInjury", strCurrentUser)

                If iresult = 0 Then
                    dsDOLF5500 = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_5500", b5KLimit)
                    dgvClean5500_BindData()

                    dsDOLF5500Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_5500", b5KLimit)
                    dgvDirty5500_BindData()

                    get_DOL_CleanStats()
                End If

                Me.Cursor = Cursors.Default
                bInitial = False
            End If

        Catch ex As Exception
            bInitial = False
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnInjury_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnInjury_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnParticipantsGreaterthan250_Click(sender As System.Object, e As System.EventArgs) Handles btnParticipantsGreaterthan250.Click
        Dim iresult As Integer
        Try
            If Not boolReadOnly Then
                bInitial = True
                Me.Cursor = Cursors.AppStarting

                'iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_RemoveNoParticipants", strCurrentUser)


                'Dim CN
                'Using conn As New SqlClient.SqlConnection("Server=NasProSQL1;Database=Testing;timeout=0")
                Using conn As New SqlClient.SqlConnection(CN)
                    conn.Open()
                    Using cm As New SqlClient.SqlCommand("emp.s_RemoveNoParticipants", conn)
                        cm.CommandType = CommandType.StoredProcedure
                        cm.CommandTimeout = 3000
                        cm.Parameters.Add("@user", SqlDbType.VarChar)
                        cm.Parameters("@user").Value = strCurrentUser

                        cm.ExecuteNonQuery()
                    End Using
                End Using


                If iresult = 0 Then
                    dsDOLF5500 = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_5500", b5KLimit)
                    dgvClean5500_BindData()

                    dsDOLF5500Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_5500", b5KLimit)
                    dgvDirty5500_BindData()

                    get_DOL_CleanStats()
                End If

                Me.Cursor = Cursors.Default
                bInitial = False
            End If

        Catch ex As Exception
            bInitial = False
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnParticipantsGreaterthan250_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnParticipantsGreaterthan250_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnValidateHealth_Click(sender As System.Object, e As System.EventArgs) Handles btnValidateHealth.Click
        Dim dsFix As New DataSet
        Try
            If Not boolReadOnly Then
                Me.Cursor = Cursors.AppStarting
                dsFix = SQLHelper.ExecuteDataset(CN, "Emp.s_Clean_Health_Ind")

                dsDOLScheduleA = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_A")
                dgvSched_A_BindData()

                dsDOLScheduleA_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_A")
                dgv_Sched_A_Drop_BindData()

                Me.Cursor = Cursors.Default

                If dsFix.Tables.Count = 0 Then
                    MsgBox("No records fixed")
                Else
                    MsgBox(CStr(dsFix.Tables(0).Rows(0).Item(0).ToString) + " records fixed.")
                End If
            End If

        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnValidateHealth_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnValidateHealth_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnNormalizePBMNames_Click(sender As System.Object, e As System.EventArgs) Handles btnNormalizePBMNames.Click
        Dim dsFix As New DataSet
        Try
            If Not boolReadOnly Then
                Me.Cursor = Cursors.AppStarting
                dsFix = SQLHelper.ExecuteDataset(CN, "Emp.s_NormalizePBMNames")

                dsDOLScheduleC = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_C")
                dgvSched_C_BindData()

                dsDOLScheduleC_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_C")
                dgv_Sched_C_Drop_BindData()

                Me.Cursor = Cursors.Default
                If dsFix.Tables.Count = 0 Then
                    MsgBox("No records fixed")
                Else
                    MsgBox(CStr(dsFix.Tables(0).Rows(0).Item(0).ToString) + " records fixed.")
                End If
            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnNormalizePBMNames_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnNormalizePBMNames_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnEliminateNonAdmin_Click(sender As System.Object, e As System.EventArgs) Handles btnEliminateNonAdmin.Click
        Dim dsFix As New DataSet
        Try
            If Not boolReadOnly Then
                Me.Cursor = Cursors.AppStarting
                dsFix = SQLHelper.ExecuteDataset(CN, "Emp.s_Eliminate_Non_Admins")

                dsDOLScheduleC = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_C")
                dgvSched_C_BindData()

                dsDOLScheduleC_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_C")
                dgv_Sched_C_Drop_BindData()

                Me.Cursor = Cursors.Default
                If dsFix.Tables.Count = 0 Then
                    MsgBox("No records fixed")
                Else
                    MsgBox(CStr(dsFix.Tables(0).Rows(0).Item(0).ToString) + " records fixed.")
                End If
            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnEliminateNonAdmin_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnEliminateNonAdmin_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnAddPBM_Click(sender As System.Object, e As System.EventArgs) Handles btnAddPBM.Click
        Dim dsFix As Int16
        Try
            If Not boolReadOnly Then
                'Me.Cursor = Cursors.AppStarting
                dsFix = SQLHelper.ExecuteScalar(CN, "Emp.s_CheckPBMAdd", cmbPBM.SelectedValue(0), txtEIN.Text)

                If dsFix = 0 Then
                    SQLHelper.ExecuteNonQuery(CN, "Emp.s_PBMAdd", cmbPBM.SelectedValue(0), txtEIN.Text)
                    MsgBox("Record Added")
                    dsPBMs = SQLHelper.ExecuteDataset(CN, "emp.s_get_PBMs")
                    dgvPBMs_BindData()
                ElseIf dsFix = 1 Then
                    MsgBox("Valid characters are 0-9")
                ElseIf dsFix = 3 Then
                    MsgBox("EIN must be 9 characters in length with leading zeros.")
                ElseIf dsFix = 2 Then
                    MsgBox("This EIN is already in the table")
                Else
                    MsgBox("Unhandled error")
                End If

            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnAddPBM_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAddPBM_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnVerifyWelfareBenefit_Click(sender As System.Object, e As System.EventArgs) Handles btnVerifyWelfareBenefit.Click
        Dim dsfix As New DataSet


        Try
            If Not boolReadOnly Then
                Me.Cursor = Cursors.AppStarting
                dsfix = SQLHelper.ExecuteDataset(CN, "Emp.s_Clean_StopLoss")

                If dsfix.Tables.Count = 0 Then
                    MsgBox("No records fixed")
                Else
                    MsgBox(CStr(dsfix.Tables(0).Rows(0).Item(0).ToString) + " records fixed.")
                End If

                dsDOLScheduleA = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_A")
                dgvSched_A_BindData()

                dsDOLScheduleA_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_A")
                dgv_Sched_A_Drop_BindData()

                Me.Cursor = Cursors.Default
            End If

        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnVerifyWelfareBenefit_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnVerifyWelfareBenefit_Click : " + ex.Message)
        End Try

    End Sub

    Private Sub btnFix_FundingGenAsset_Click(sender As System.Object, e As System.EventArgs) Handles btnFix_FundingGenAsset.Click
        Dim dsFix As New DataSet
        Try
            If Not boolReadOnly Then
                Me.Cursor = Cursors.AppStarting
                dsFix = SQLHelper.ExecuteDataset(CN, "Emp.s_Clean_ScheduleA_FundingGenAsset")

                Me.Cursor = Cursors.Default
                If dsFix.Tables.Count = 0 Then
                    MsgBox("No records fixed")
                Else
                    MsgBox(CStr(dsFix.Tables(0).Rows(0).Item(0).ToString) + " records fixed.")
                End If
            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnFix_FundingGenAsset_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnFix_FundingGenAsset_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnExperience_Click(sender As System.Object, e As System.EventArgs) Handles btnExperience.Click
        Dim dsFix As New DataSet
        Try
            If Not boolReadOnly Then
                Me.Cursor = Cursors.AppStarting
                dsFix = SQLHelper.ExecuteDataset(CN, "Emp.s_Clean_ScheduleA_Experience")

                dsDOLScheduleA = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_A")
                dgvSched_A_BindData()

                dsDOLScheduleA_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_A")
                dgv_Sched_A_Drop_BindData()

                Me.Cursor = Cursors.Default
                If dsFix.Tables.Count = 0 Then
                    MsgBox("No records fixed")
                Else
                    MsgBox(CStr(dsFix.Tables(0).Rows(0).Item(0).ToString) + " records fixed.")
                End If
            End If

        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnExperience_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnExperience_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnRemove5500s_Click(sender As System.Object, e As System.EventArgs) Handles btnRemove5500s.Click
        Dim iresult As Integer

        Try
            If Not boolReadOnly Then
                iresult = MsgBox("This will delete all of the 5500s that are marked for deletion along with their associated Schedule A and C's  Are you sure you want to do this?", MsgBoxStyle.YesNo)
                If iresult = 6 Then
                    Me.Cursor = Cursors.AppStarting
                    Using conn As New SqlClient.SqlConnection(CN)
                        conn.Open()
                        Using cm As New SqlClient.SqlCommand("emp.s_Delete_5500s", conn)
                            cm.CommandType = CommandType.StoredProcedure
                            cm.CommandTimeout = 500
                            cm.ExecuteNonQuery()
                        End Using
                    End Using

                    dsDOLF5500 = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_5500", b5KLimit)
                    dgvClean5500_BindData()

                    dsDOLF5500Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_5500", b5KLimit)
                    dgvDirty5500_BindData()

                    dsDOLScheduleA = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_A")
                    dgvSched_A_BindData()

                    dsDOLScheduleA_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_A")
                    dgv_Sched_A_Drop_BindData()

                    dsDOLScheduleC = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_C")
                    dgvSched_C_BindData()

                    dsDOLScheduleC_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_C")
                    dgv_Sched_C_Drop_BindData()

                    Me.Cursor = Cursors.Default
                End If
            End If
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "btnRemove5500s_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnRemove5500s_Click : " + ex.Message)
            Me.Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub btnFlexPlans_Click(sender As System.Object, e As System.EventArgs) Handles btnFlexPlans.Click
        Dim iresult As Integer
        Try
            If Not boolReadOnly Then
                bInitial = True
                Me.Cursor = Cursors.AppStarting

                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_RemoveFlex_ReimbursementPlans", strCurrentUser)

                If iresult = 0 Then
                    dsDOLF5500 = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_5500", b5KLimit)
                    dgvClean5500_BindData()

                    dsDOLF5500Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_5500", b5KLimit)
                    dgvDirty5500_BindData()

                    get_DOL_CleanStats()
                End If

                Me.Cursor = Cursors.Default
                bInitial = False
            End If

        Catch ex As Exception
            bInitial = False
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnFlexPlans_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnFlexPlans_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnProviderEIN_Click(sender As System.Object, e As System.EventArgs) Handles btnProviderEIN.Click
        Dim dsfix As New DataSet


        Try
            If Not boolReadOnly Then
                Me.Cursor = Cursors.AppStarting

                dsfix = SQLHelper.ExecuteDataset(CN, "Emp.s_Clean_ScheduleC_NoProviderEIN")

                If dsfix.Tables.Count = 0 Then
                    MsgBox("No records fixed")
                Else
                    MsgBox(CStr(dsfix.Tables(0).Rows(0).Item(0).ToString) + " records fixed.")
                End If

                dsDOLScheduleC = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_C")
                dgvSched_C_BindData()

                dsDOLScheduleC_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_C")
                dgv_Sched_C_Drop_BindData()

                Me.Cursor = Cursors.Default
            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnVerifyWelfareBenefit_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnVerifyWelfareBenefit_Click : " + ex.Message)
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

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colStatusCode As New DataGridViewTextBoxColumn
            With colStatusCode
                .DataPropertyName = "Status Code"
                .HeaderText = "Status Code"
                .Name = "Status Code"
                .Width = 53
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgvCorporate.Columns.Add(colStatusCode)


            'Set DataGridView textbox Column for ImportAnalysis
            Dim colSubsidiaryCode As New DataGridViewTextBoxColumn
            With colSubsidiaryCode
                .DataPropertyName = "Subsidiary Code"
                .HeaderText = "Subsidiary Code"
                .Name = "Subsidiary Code"
                .Width = 65
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgvCorporate.Columns.Add(colSubsidiaryCode)


            'Set DataGridView textbox Column for ImportAnalysis
            Dim colHierarchyCode As New DataGridViewTextBoxColumn
            With colHierarchyCode
                .DataPropertyName = "Hierarchy Code"
                .HeaderText = "Hierarchy Code"
                .Name = "Hierarchy Code"
                .Width = 65
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgvCorporate.Columns.Add(colHierarchyCode)

            ''don't allow columns to be sorted
            'Dim i As Integer
            'For i = 0 To dgvCorporate.Columns.Count - 1
            '    dgvCorporate.Columns.Item(i).SortMode = DataGridViewColumnSortMode.NotSortable
            '    dgvCorporate.Columns.Item(i).ReadOnly = True
            'Next
        Catch ex As Exception
            Functions.Sendmail(ex.Message, "dgvCorporate_FormatGrid", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvCorporate_FormatGrid " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvCorporate_BindData()
        Try
            dgvCorporate.Rows.Clear()
            For i As Integer = 0 To dsCorporateList.Tables(0).Rows.Count - 1
                Me.dgvCorporate.Rows.Add(dsCorporateList.Tables(0).Rows(i).Item(0), dsCorporateList.Tables(0).Rows(i).Item(1), _
                                    dsCorporateList.Tables(0).Rows(i).Item(2), dsCorporateList.Tables(0).Rows(i).Item(3), _
                                    dsCorporateList.Tables(0).Rows(i).Item(4), dsCorporateList.Tables(0).Rows(i).Item(5), _
                                    dsCorporateList.Tables(0).Rows(i).Item(6), dsCorporateList.Tables(0).Rows(i).Item(7), _
                                    dsCorporateList.Tables(0).Rows(i).Item(8), dsCorporateList.Tables(0).Rows(i).Item(9), _
                                    dsCorporateList.Tables(0).Rows(i).Item(10))

            Next
        Catch ex As Exception
            Functions.Sendmail(ex.Message, "dgvCorporate_BindData", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvCorporate_BindData  : " + ex.Message)
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
                .RowHeadersWidth = 32
            End With
            'Set DataGridView textbox Column for Duns
            Dim colDUNS As New DataGridViewTextBoxColumn
            With colDUNS
                .DataPropertyName = "DUNS"
                .Name = "DUNS"
                .Visible = True
                .Width = 75
            End With
            dgvSubsidiaryBrowser.Columns.Add(colDUNS)

            'Set DataGridView textbox Column for EmployerID
            Dim colEmployerID As New DataGridViewTextBoxColumn
            With colEmployerID
                .DataPropertyName = "EmployerID"
                '.HeaderText = "MCO Name"
                .Name = "EmployerID"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .Width = 70
            End With
            dgvSubsidiaryBrowser.Columns.Add(colEmployerID)


            'Set DataGridView textbox Column for Business Name
            Dim colBusinessName As New DataGridViewTextBoxColumn
            With colBusinessName
                .DataPropertyName = "BusinessName"
                .HeaderText = "Business Name"
                .Name = "BusinessName"
                .Width = 270
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
                .Width = 245
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
                .Width = 125
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
                .Width = 40
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
                .Width = 75
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

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colStatusCode As New DataGridViewTextBoxColumn
            With colStatusCode
                .DataPropertyName = "Status Code"
                .HeaderText = "Status Code"
                .Name = "Status Code"
                .Width = 53
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgvSubsidiaryBrowser.Columns.Add(colStatusCode)


            'Set DataGridView textbox Column for ImportAnalysis
            Dim colSubsidiaryCode As New DataGridViewTextBoxColumn
            With colSubsidiaryCode
                .DataPropertyName = "Subsidiary Code"
                .HeaderText = "Subsidiary Code"
                .Name = "Subsidiary Code"
                .Width = 65
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgvSubsidiaryBrowser.Columns.Add(colSubsidiaryCode)


            'Set DataGridView textbox Column for ImportAnalysis
            Dim colHierarchyCode As New DataGridViewTextBoxColumn
            With colHierarchyCode
                .DataPropertyName = "Hierarchy Code"
                .HeaderText = "Hierarchy Code"
                .Name = "Hierarchy Code"
                .Width = 65
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgvSubsidiaryBrowser.Columns.Add(colHierarchyCode)

        Catch ex As Exception
            Functions.Sendmail(ex.Message, "dgvSubsidiaryBrowser_FormatGrid", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvSubsidiaryBrowser_FormatGrid  : " + ex.Message)
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
                                        dsSubsidiaryList.Tables(0).Rows(i).Item(8), dsSubsidiaryList.Tables(0).Rows(i).Item(9), _
                                        dsSubsidiaryList.Tables(0).Rows(i).Item(10), dsSubsidiaryList.Tables(0).Rows(i).Item(11))
                Next
            Else
                TextBox90.Text = "None"
            End If
        Catch ex As Exception
            Functions.Sendmail(ex.Message, "dgvSubsidiaryBrowser_BindData", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvSubsidiaryBrowser_BindData  : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvClean5500_FormatGrid()
        'This is a general formatting grid subroutine for the HMO, PPO, HMOMedicare, HMOMedicaid datagrids
        Try
            'set Visual Basic Datagrid Header style to false so we can use our own
            'The key statement required to get the column and row styles to work
            'Visual Header styles must be shut off
            dgvClean5500.EnableHeadersVisualStyles = False
            'go and set the styles
            With dgvClean5500
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
            Dim colDOLID As New DataGridViewTextBoxColumn
            With colDOLID
                .DataPropertyName = "DOLID"
                .Name = "DOLID"
                .Visible = False
                .Width = 78
            End With
            dgvClean5500.Columns.Add(colDOLID)

            'Set DataGridView textbox Column for FORM_PLAN_YEAR_BEGIN_DATE
            Dim colFORM_PLAN_YEAR_BEGIN_DATE As New DataGridViewTextBoxColumn
            With colFORM_PLAN_YEAR_BEGIN_DATE
                .DataPropertyName = "FORM_PLAN_YEAR_BEGIN_DATE"
                .HeaderText = "Plan Year"
                .Name = "FORM_PLAN_YEAR_BEGIN_DATE"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Format = "MM/dd/yyyy"
                .Width = 80
            End With
            dgvClean5500.Columns.Add(colFORM_PLAN_YEAR_BEGIN_DATE)


            'Set DataGridView textbox Column for SPONS_DFE_EIN
            Dim colSPONS_DFE_EIN As New DataGridViewTextBoxColumn
            With colSPONS_DFE_EIN
                .DataPropertyName = "SPONS_DFE_EIN"
                .HeaderText = "EIN"
                .Name = "SPONS_DFE_EIN"
                .Width = 75
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvClean5500.Columns.Add(colSPONS_DFE_EIN)

            'Set DataGridView textbox Column for Business Name
            Dim colSPONS_DFE_PN As New DataGridViewTextBoxColumn
            With colSPONS_DFE_PN
                .DataPropertyName = "SPONS_DFE_PN"
                .HeaderText = "PN"
                .Name = "SPONS_DFE_PN"
                .Width = 55
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##,##0"
            End With
            dgvClean5500.Columns.Add(colSPONS_DFE_PN)


            'Set DataGridView textbox Column for PLAN_NAME
            Dim colPLAN_NAME As New DataGridViewTextBoxColumn
            With colPLAN_NAME
                .DataPropertyName = "PLAN_NAME"
                .HeaderText = "PLAN NAME"
                .Name = "PLAN_NAME"
                .Width = 330
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvClean5500.Columns.Add(colPLAN_NAME)

            'Set DataGridView textbox Column for SUBTL_ACT_RTD_SEP_CNT
            Dim colSUBTL_ACT_RTD_SEP_CNT As New DataGridViewTextBoxColumn
            With colSUBTL_ACT_RTD_SEP_CNT
                .DataPropertyName = "SUBTL_ACT_RTD_SEP_CNT"
                .HeaderText = "Participants"
                .Name = "SUBTL_ACT_RTD_SEP_CNT"
                .Width = 70
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvClean5500.Columns.Add(colSUBTL_ACT_RTD_SEP_CNT)


            'Set DataGridView textbox Column for TYPE_WELFARE_BNFT_CODE
            Dim colTYPE_WELFARE_BNFT_CODE As New DataGridViewTextBoxColumn
            With colTYPE_WELFARE_BNFT_CODE
                .DataPropertyName = "TYPE_WELFARE_BNFT_CODE"
                .HeaderText = "Benefit Code"
                .Name = "TYPE_WELFARE_BNFT_CODE"
                .Width = 125
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvClean5500.Columns.Add(colTYPE_WELFARE_BNFT_CODE)

            'Set DataGridView textbox Column for [FUNDING_INSURANCE_IND]
            Dim colFunding_Insurance_Ind As New DataGridViewTextBoxColumn
            With colFunding_Insurance_Ind
                .DataPropertyName = "FUNDING_INSURANCE_IND"
                .HeaderText = "Funding Insurance"
                .Name = "FUNDING_INSURANCE_IND"
                .Width = 70
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "MM/dd/yyyy"
            End With
            dgvClean5500.Columns.Add(colFunding_Insurance_Ind)

            'Set DataGridView textbox Column for ACK_ID
            Dim colACK_ID As New DataGridViewTextBoxColumn
            With colACK_ID
                .DataPropertyName = "ACK_ID"
                .HeaderText = "ACK_ID"
                .Name = "ACK_ID"
                .Width = 225
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvClean5500.Columns.Add(colACK_ID)

            'Set DataGridView textbox Column for Dailytimedimid
            Dim colDailytimedimid As New DataGridViewTextBoxColumn
            With colDailytimedimid
                .DataPropertyName = "Dailytimedimid"
                .HeaderText = "Date ID"
                .Name = "Dailytimedimid"
                .Width = 55
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvClean5500.Columns.Add(colDailytimedimid)

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colPLAN_EFF_DATE As New DataGridViewTextBoxColumn
            With colPLAN_EFF_DATE
                .DataPropertyName = "PLAN_EFF_DATE"
                .HeaderText = "Effective Date"
                .Name = "PLAN_EFF_DATE"
                .Width = 70
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "MM/dd/yyyy"
            End With
            dgvClean5500.Columns.Add(colPLAN_EFF_DATE)

        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvClean5500_FormatGrid", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvClean5500_FormatGrid " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvClean5500_BindData()
        Try
            dgvClean5500.Rows.Clear()
            For i As Integer = 0 To dsDOLF5500.Tables(0).Rows.Count - 1
                Me.dgvClean5500.Rows.Add(dsDOLF5500.Tables(0).Rows(i).Item(0), dsDOLF5500.Tables(0).Rows(i).Item(1), _
                                    dsDOLF5500.Tables(0).Rows(i).Item(3), dsDOLF5500.Tables(0).Rows(i).Item(2), _
                                    dsDOLF5500.Tables(0).Rows(i).Item(4), dsDOLF5500.Tables(0).Rows(i).Item(8), _
                                    dsDOLF5500.Tables(0).Rows(i).Item(9), dsDOLF5500.Tables(0).Rows(i).Item(10), _
                                    dsDOLF5500.Tables(0).Rows(i).Item(5), _
                                    dsDOLF5500.Tables(0).Rows(i).Item(6), dsDOLF5500.Tables(0).Rows(i).Item(7))
            Next
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvClean5500_BindData", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvClean5500_BindData  : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvDirty5500_FormatGrid()
        'This is a general formatting grid subroutine for the HMO, PPO, HMOMedicare, HMOMedicaid datagrids
        Try
            'set Visual Basic Datagrid Header style to false so we can use our own
            'The key statement required to get the column and row styles to work
            'Visual Header styles must be shut off
            dgvDirty5500.EnableHeadersVisualStyles = False
            'go and set the styles
            With dgvDirty5500
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
            Dim colDOLID As New DataGridViewTextBoxColumn
            With colDOLID
                .DataPropertyName = "DOLID"
                .Name = "DOLID"
                .Visible = False
                .Width = 78
            End With
            dgvDirty5500.Columns.Add(colDOLID)

            'Set DataGridView textbox Column for Analysis
            Dim colAnalysis As New DataGridViewTextBoxColumn
            With colAnalysis
                .DataPropertyName = "Analysis"
                .HeaderText = "Analysis"
                .Name = "Analysis"
                .Width = 95
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDirty5500.Columns.Add(colAnalysis)

            'Set DataGridView textbox Column for FORM_PLAN_YEAR_BEGIN_DATE
            Dim colFORM_PLAN_YEAR_BEGIN_DATE As New DataGridViewTextBoxColumn
            With colFORM_PLAN_YEAR_BEGIN_DATE
                .DataPropertyName = "FORM_PLAN_YEAR_BEGIN_DATE"
                .HeaderText = "Plan Year"
                .Name = "FORM_PLAN_YEAR_BEGIN_DATE"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Format = "MM/dd/yyyy"
                .Width = 80
            End With
            dgvDirty5500.Columns.Add(colFORM_PLAN_YEAR_BEGIN_DATE)


            'Set DataGridView textbox Column for SPONS_DFE_EIN
            Dim colSPONS_DFE_EIN As New DataGridViewTextBoxColumn
            With colSPONS_DFE_EIN
                .DataPropertyName = "SPONS_DFE_EIN"
                .HeaderText = "EIN"
                .Name = "SPONS_DFE_EIN"
                .Width = 75
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDirty5500.Columns.Add(colSPONS_DFE_EIN)

            'Set DataGridView textbox Column for Business Name
            Dim colSPONS_DFE_PN As New DataGridViewTextBoxColumn
            With colSPONS_DFE_PN
                .DataPropertyName = "SPONS_DFE_PN"
                .HeaderText = "PN"
                .Name = "SPONS_DFE_PN"
                .Width = 55
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##,##0"
            End With
            dgvDirty5500.Columns.Add(colSPONS_DFE_PN)


            'Set DataGridView textbox Column for PLAN_NAME
            Dim colPLAN_NAME As New DataGridViewTextBoxColumn
            With colPLAN_NAME
                .DataPropertyName = "PLAN_NAME"
                .HeaderText = "PLAN NAME"
                .Name = "PLAN_NAME"
                .Width = 330
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDirty5500.Columns.Add(colPLAN_NAME)

            'Set DataGridView textbox Column for SUBTL_ACT_RTD_SEP_CNT
            Dim colSUBTL_ACT_RTD_SEP_CNT As New DataGridViewTextBoxColumn
            With colSUBTL_ACT_RTD_SEP_CNT
                .DataPropertyName = "SUBTL_ACT_RTD_SEP_CNT"
                .HeaderText = "Participants"
                .Name = "SUBTL_ACT_RTD_SEP_CNT"
                .Width = 70
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDirty5500.Columns.Add(colSUBTL_ACT_RTD_SEP_CNT)

            'Set DataGridView textbox Column for TYPE_WELFARE_BNFT_CODE
            Dim colTYPE_WELFARE_BNFT_CODE As New DataGridViewTextBoxColumn
            With colTYPE_WELFARE_BNFT_CODE
                .DataPropertyName = "TYPE_WELFARE_BNFT_CODE"
                .HeaderText = "Benefit Code"
                .Name = "TYPE_WELFARE_BNFT_CODE"
                .Width = 100
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDirty5500.Columns.Add(colTYPE_WELFARE_BNFT_CODE)

            'Set DataGridView textbox Column for Form_Tax_Prd
            Dim colFunding_Insurance_Ind As New DataGridViewTextBoxColumn
            With colFunding_Insurance_Ind
                .DataPropertyName = "FUNDING_INSURANCE_IND"
                .HeaderText = "Funding Insurance"
                .Name = "FUNDING_INSURANCE_IND"
                .Width = 70
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "MM/dd/yyyy"
            End With
            dgvDirty5500.Columns.Add(colFunding_Insurance_Ind)

            'Set DataGridView textbox Column for ACK_ID
            Dim colACK_ID As New DataGridViewTextBoxColumn
            With colACK_ID
                .DataPropertyName = "ACK_ID"
                .HeaderText = "ACK_ID"
                .Name = "ACK_ID"
                .Width = 150
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDirty5500.Columns.Add(colACK_ID)

            'Set DataGridView textbox Column for Dailytimedimid
            Dim colDailytimedimid As New DataGridViewTextBoxColumn
            With colDailytimedimid
                .DataPropertyName = "Dailytimedimid"
                .HeaderText = "Date ID"
                .Name = "Dailytimedimid"
                .Width = 60
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDirty5500.Columns.Add(colDailytimedimid)

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colPLAN_EFF_DATE As New DataGridViewTextBoxColumn
            With colPLAN_EFF_DATE
                .DataPropertyName = "PLAN_EFF_DATE"
                .HeaderText = "Effective Date"
                .Name = "PLAN_EFF_DATE"
                .Width = 70
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "MM/dd/yyyy"
            End With
            dgvDirty5500.Columns.Add(colPLAN_EFF_DATE)

        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvDirty5500_FormatGrid", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvDirty5500_FormatGrid " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvDirty5500_BindData()
        Try
            dgvDirty5500.Rows.Clear()
            For i As Integer = 0 To dsDOLF5500Deletes.Tables(0).Rows.Count - 1
                Me.dgvDirty5500.Rows.Add(dsDOLF5500Deletes.Tables(0).Rows(i).Item(0), dsDOLF5500Deletes.Tables(0).Rows(i).Item(9), _
                                    dsDOLF5500Deletes.Tables(0).Rows(i).Item(1), _
                                    dsDOLF5500Deletes.Tables(0).Rows(i).Item(3), dsDOLF5500Deletes.Tables(0).Rows(i).Item(2), _
                                    dsDOLF5500Deletes.Tables(0).Rows(i).Item(4), dsDOLF5500Deletes.Tables(0).Rows(i).Item(8), _
                                    dsDOLF5500Deletes.Tables(0).Rows(i).Item(10), dsDOLF5500Deletes.Tables(0).Rows(i).Item(11), _
                                    dsDOLF5500Deletes.Tables(0).Rows(i).Item(5), _
                                    dsDOLF5500Deletes.Tables(0).Rows(i).Item(6), dsDOLF5500Deletes.Tables(0).Rows(i).Item(7))
            Next
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvDirty5500_BindData", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvDirty5500_BindData  : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvClean5500_UserDeletingRow(ByVal sender As System.Object, e As DataGridViewRowCancelEventArgs) Handles dgvClean5500.UserDeletingRow
        Dim iResult As Integer, sortColumn As DataGridViewColumn, myindex As Integer, mysortcolumn As Integer
        Dim SetSortOrder As ListSortDirection
        Dim GridSortOrder As SortOrder

        Try
            e.Cancel = True ' never actually delete the row.
            If Not bInitial And Not boolReadOnly Then
                bInitial = True
                If dgvClean5500.SelectedRows.Count > 1 Then
                    MsgBox("please only select 1 row to delete at a time")

                Else

                    sortColumn = dgvClean5500.SortedColumn


                    If Not sortColumn Is Nothing Then
                        mysortcolumn = sortColumn.Index
                        GridSortOrder = dgvClean5500.SortOrder
                    End If

                    myindex = dgvClean5500.CurrentRow.Index

                    iResult = SQLHelper.ExecuteScalar(CN, "Emp.s_UserDelete_5500", _
                                                      dgvClean5500.SelectedRows(0).Cells("SPONS_DFE_EIN").Value.ToString, _
                                                      dgvClean5500.SelectedRows(0).Cells("SPONS_DFE_PN").Value.ToString,
                                                      Userid)
                    If iResult = 0 Then
                        MsgBox("Record marked for deletion")
                    Else
                        MsgBox("Delete failed")
                    End If


                    Me.Cursor = Cursors.AppStarting

                    dsDOLF5500 = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_5500", b5KLimit)
                    dgvClean5500_BindData()

                    dsDOLF5500Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_5500", b5KLimit)
                    dgvDirty5500_BindData()

                    get_DOL_CleanStats()

                    If GridSortOrder = Windows.Forms.SortOrder.Ascending Then
                        SetSortOrder = ListSortDirection.Ascending
                    ElseIf GridSortOrder = Windows.Forms.SortOrder.Descending Then
                        SetSortOrder = ListSortDirection.Descending
                    ElseIf GridSortOrder = Windows.Forms.SortOrder.None Then
                        SetSortOrder = ListSortDirection.Ascending
                    Else : GridSortOrder = ListSortDirection.Ascending
                        MsgBox("not good")
                    End If

                    If Not sortColumn Is Nothing Then
                        dgvClean5500.Sort(sortColumn, SetSortOrder)
                        Me.dgvClean5500.CurrentCell = Me.dgvClean5500(mysortcolumn, myindex)
                    End If



                    Me.Cursor = Cursors.Default
                    bInitial = False
                End If
            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "dgvClean5500_UserDeletingRow ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvClean5500_UserDeletingRow : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvdirty5500_UserDeletingRow(ByVal sender As System.Object, e As DataGridViewRowCancelEventArgs) Handles dgvDirty5500.UserDeletingRow
        Dim iResult As Integer, sortColumn As DataGridViewColumn, myindex As Integer, mysortcolumn As Integer
        Dim SetSortOrder As ListSortDirection
        Dim GridSortOrder As SortOrder

        Try
            e.Cancel = True ' never actually delete the row.
            If Not bInitial And Not boolReadOnly Then
                bInitial = True
                If dgvDirty5500.SelectedRows.Count > 1 Then
                    MsgBox("please only select 1 row to delete at a time")
                Else

                    sortColumn = dgvClean5500.SortedColumn

                    If Not sortColumn Is Nothing Then
                        mysortcolumn = sortColumn.Index
                        GridSortOrder = dgvDirty5500.SortOrder
                    End If

                    myindex = dgvDirty5500.CurrentRow.Index

                    iResult = SQLHelper.ExecuteScalar(CN, "Emp.s_User_UnDelete_5500", _
                                                      dgvDirty5500.SelectedRows(0).Cells("SPONS_DFE_EIN").Value.ToString, _
                                                      dgvDirty5500.SelectedRows(0).Cells("SPONS_DFE_PN").Value.ToString,
                                                      Userid)
                    If iResult = 0 Then
                        MsgBox("Record unmarked for deletion")
                    Else
                        MsgBox("Undelete failed")
                    End If


                    Me.Cursor = Cursors.AppStarting

                    dsDOLF5500 = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_5500", b5KLimit)
                    dgvClean5500_BindData()

                    dsDOLF5500Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_5500", b5KLimit)
                    dgvDirty5500_BindData()

                    get_DOL_CleanStats()

                    If GridSortOrder = Windows.Forms.SortOrder.Ascending Then
                        SetSortOrder = ListSortDirection.Ascending
                    ElseIf GridSortOrder = Windows.Forms.SortOrder.Descending Then
                        SetSortOrder = ListSortDirection.Descending
                    ElseIf GridSortOrder = Windows.Forms.SortOrder.None Then
                        SetSortOrder = ListSortDirection.Ascending
                    Else : GridSortOrder = ListSortDirection.Ascending
                        MsgBox("not good")
                    End If

                    If Not sortColumn Is Nothing Then
                        dgvDirty5500.Sort(sortColumn, SetSortOrder)
                        Me.dgvDirty5500.CurrentCell = Me.dgvDirty5500(mysortcolumn, myindex)
                    End If
                    Me.Cursor = Cursors.Default
                    bInitial = False
                End If

            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "dgvDirty5500_UserUnDeletingRow ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvDirty5500_UserUnDeletingRow : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvSched_A_FormatGrid()
        'This is a general formatting grid subroutine for the HMO, PPO, HMOMedicare, HMOMedicaid datagrids
        Try
            'set Visual Basic Datagrid Header style to false so we can use our own
            'The key statement required to get the column and row styles to work
            'Visual Header styles must be shut off
            dgvSched_A.EnableHeadersVisualStyles = False
            'go and set the styles
            With dgvSched_A
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
                .RowHeadersWidth = 30
            End With
            'Set DataGridView textbox Column for Duns
            Dim colDOLID As New DataGridViewTextBoxColumn
            With colDOLID
                .DataPropertyName = "DOLID"
                .Name = "DOLID"
                .Width = 55
            End With
            dgvSched_A.Columns.Add(colDOLID)

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colFormID As New DataGridViewTextBoxColumn
            With colFormID
                .DataPropertyName = "Form_ID"
                .HeaderText = "Form ID"
                .Name = "Form_ID"
                .Width = 50
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                .Visible = False
            End With
            dgvSched_A.Columns.Add(colFormID)

            'Set DataGridView textbox Column for ACK_ID
            Dim colACK_ID As New DataGridViewTextBoxColumn
            With colACK_ID
                .DataPropertyName = "ACK_ID"
                .HeaderText = "ACK_ID"
                .Name = "ACK_ID"
                .Width = 225
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvSched_A.Columns.Add(colACK_ID)

            'Set DataGridView textbox Column for Carrier
            Dim colCarrier As New DataGridViewTextBoxColumn
            With colCarrier
                .DataPropertyName = "Carrier"
                .HeaderText = "Carrier"
                .Name = "Carrier"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .Width = 300
            End With
            dgvSched_A.Columns.Add(colCarrier)


            'Set DataGridView textbox Column for NAIC_Code
            Dim colNAIC_Code As New DataGridViewTextBoxColumn
            With colNAIC_Code
                .DataPropertyName = "NAIC_Code"
                .HeaderText = "NAIC Code"
                .Name = "NAIC_Code"
                .Width = 55
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            End With
            dgvSched_A.Columns.Add(colNAIC_Code)

            'Set DataGridView textbox Column for Business Name
            Dim colHealth As New DataGridViewTextBoxColumn
            With colHealth
                .DataPropertyName = "Health"
                .HeaderText = "Health"
                .Name = "Health"
                .Width = 50
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            End With
            dgvSched_A.Columns.Add(colHealth)


            'Set DataGridView textbox Column for Vision
            Dim colVision As New DataGridViewTextBoxColumn
            With colVision
                .DataPropertyName = "Vision"
                .HeaderText = "Vision"
                .Name = "Vision"
                .Width = 50
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgvSched_A.Columns.Add(colVision)

            'Set DataGridView textbox Column for Dental
            Dim colDental As New DataGridViewTextBoxColumn
            With colDental
                .DataPropertyName = "Dental"
                .HeaderText = "Dental"
                .Name = "Dental"
                .Width = 50
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgvSched_A.Columns.Add(colDental)

            'Set DataGridView textbox Column for StopLoss
            Dim colStopLoss As New DataGridViewTextBoxColumn
            With colStopLoss
                .DataPropertyName = "StopLoss"
                .HeaderText = "Stop Loss"
                .Name = "StopLoss"
                .Width = 50
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvSched_A.Columns.Add(colStopLoss)


            'Set DataGridView textbox Column for HMO
            Dim colHMO As New DataGridViewTextBoxColumn
            With colHMO
                .DataPropertyName = "HMO"
                .HeaderText = "HMO"
                .Name = "HMO"
                .Width = 50
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvSched_A.Columns.Add(colHMO)

            'Set DataGridView textbox Column for PPO
            Dim colPPO As New DataGridViewTextBoxColumn
            With colPPO
                .DataPropertyName = "PPO"
                .HeaderText = "PPO"
                .Name = "PPO"
                .Width = 50
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "MM/dd/yyyy"
            End With
            dgvSched_A.Columns.Add(colPPO)

            'Set DataGridView textbox Column for Indemnity
            Dim colIndemnity As New DataGridViewTextBoxColumn
            With colIndemnity
                .DataPropertyName = "Indemnity"
                .HeaderText = "Indemnity"
                .Name = "Indemnity"
                .Width = 55
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "MM/dd/yyyy"
            End With
            dgvSched_A.Columns.Add(colIndemnity)

            'Set DataGridView textbox Column for Drug
            Dim colDrug As New DataGridViewTextBoxColumn
            With colDrug
                .DataPropertyName = "Drug"
                .HeaderText = "Drug"
                .Name = "Drug"
                .Width = 50
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvSched_A.Columns.Add(colDrug)

            'Set DataGridView textbox Column for INS_PRSN_COVERED_EOY_CNT
            Dim colINS_PRSN_COVERED_EOY_CNT As New DataGridViewTextBoxColumn
            With colINS_PRSN_COVERED_EOY_CNT
                .DataPropertyName = "INS_PRSN_COVERED_EOY_CNT"
                .HeaderText = "Covered Count"
                .Name = "INS_PRSN_COVERED_EOY_CNT"
                .Width = 62
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "##,##0"
            End With
            dgvSched_A.Columns.Add(colINS_PRSN_COVERED_EOY_CNT)

            'Set DataGridView textbox Column for ExperienceRatedPremiumsPMPM
            Dim colExperienceRatedPremiumsPMPM As New DataGridViewTextBoxColumn
            With colExperienceRatedPremiumsPMPM
                .DataPropertyName = "ExperienceRatedPremiumsPMPM"
                .HeaderText = "Experience Rated Premiums PMPM"
                .Name = "ExperienceRatedPremiumsPMPM"
                .Width = 80
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "c0"
            End With
            dgvSched_A.Columns.Add(colExperienceRatedPremiumsPMPM)

            'Set DataGridView textbox Column for NonExperienceRatedPremiumsPMPM
            Dim colNonExperienceRatedPremiumsPMPM As New DataGridViewTextBoxColumn
            With colNonExperienceRatedPremiumsPMPM
                .DataPropertyName = "NonExperienceRatedPremiumsPMPM"
                .HeaderText = "Non Experience Rated Premiums PMPM"
                .Name = "NonExperienceRatedPremiumsPMPM"
                .Width = 80
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "c0"
            End With
            dgvSched_A.Columns.Add(colNonExperienceRatedPremiumsPMPM)

            'Set DataGridView textbox Column for ImportAnalysis
            Dim colOtherText As New DataGridViewTextBoxColumn
            With colOtherText
                .DataPropertyName = "OtherText"
                .HeaderText = "Other Text"
                .Name = "OtherText"
                .Width = 300
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "MM/dd/yyyy"
            End With
            dgvSched_A.Columns.Add(colOtherText)

        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvSched_A_FormatGrid", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvSched_A_FormatGrid " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvSched_A_BindData()
        Try
            dgvSched_A.Rows.Clear()
            For i As Integer = 0 To dsDOLScheduleA.Tables(0).Rows.Count - 1
                Me.dgvSched_A.Rows.Add(dsDOLScheduleA.Tables(0).Rows(i).Item(0), dsDOLScheduleA.Tables(0).Rows(i).Item(16), _
                                    dsDOLScheduleA.Tables(0).Rows(i).Item(15), _
                                    dsDOLScheduleA.Tables(0).Rows(i).Item(1), dsDOLScheduleA.Tables(0).Rows(i).Item(2), _
                                    dsDOLScheduleA.Tables(0).Rows(i).Item(3), dsDOLScheduleA.Tables(0).Rows(i).Item(4), _
                                    dsDOLScheduleA.Tables(0).Rows(i).Item(5), dsDOLScheduleA.Tables(0).Rows(i).Item(6), _
                                    dsDOLScheduleA.Tables(0).Rows(i).Item(7), dsDOLScheduleA.Tables(0).Rows(i).Item(8), _
                                    dsDOLScheduleA.Tables(0).Rows(i).Item(9), dsDOLScheduleA.Tables(0).Rows(i).Item(10), _
                                    dsDOLScheduleA.Tables(0).Rows(i).Item(12), dsDOLScheduleA.Tables(0).Rows(i).Item(13), _
                                    dsDOLScheduleA.Tables(0).Rows(i).Item(14), dsDOLScheduleA.Tables(0).Rows(i).Item(11))
            Next
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvSched_A_BindData", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvSched_A_BindData  : " + ex.Message)
        End Try
    End Sub

    Private Sub dgv_Sched_A_Drop_FormatGrid()
        'This is a general formatting grid subroutine for the HMO, PPO, HMOMedicare, HMOMedicaid datagrids
        Try
            'set Visual Basic Datagrid Header style to false so we can use our own
            'The key statement required to get the column and row styles to work
            'Visual Header styles must be shut off
            dgv_Sched_A_Drop.EnableHeadersVisualStyles = False
            'go and set the styles
            With dgv_Sched_A_Drop
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
            Dim colDOLID As New DataGridViewTextBoxColumn
            With colDOLID
                .DataPropertyName = "DOLID"
                .Name = "DOLID"
                .Visible = False
                .Width = 78
            End With
            dgv_Sched_A_Drop.Columns.Add(colDOLID)

            'Set DataGridView textbox Column for Form_ID
            Dim colForm_ID As New DataGridViewTextBoxColumn
            With colForm_ID
                .DataPropertyName = "Form_ID"
                .HeaderText = "Form ID"
                .Name = "Form_ID"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .Width = 55
            End With
            dgv_Sched_A_Drop.Columns.Add(colForm_ID)


            'Set DataGridView textbox Column for DataDate
            Dim colDataDate As New DataGridViewTextBoxColumn
            With colDataDate
                .DataPropertyName = "DataDate"
                .HeaderText = "Data Date"
                .Name = "DataDate"
                .Width = 60
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgv_Sched_A_Drop.Columns.Add(colDataDate)

            'Set DataGridView textbox Column for Business Name
            Dim colBeginDate As New DataGridViewTextBoxColumn
            With colBeginDate
                .DataPropertyName = "BeginDate"
                .HeaderText = "Begin Date"
                .Name = "BeginDate"
                .Width = 75
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##,##0"
            End With
            dgv_Sched_A_Drop.Columns.Add(colBeginDate)


            'Set DataGridView textbox Column for CarrierName
            Dim colCarrierName As New DataGridViewTextBoxColumn
            With colCarrierName
                .DataPropertyName = "CarrierName"
                .HeaderText = "Carrier Name"
                .Name = "CarrierName"
                .Width = 450
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgv_Sched_A_Drop.Columns.Add(colCarrierName)

            'Set DataGridView textbox Column for EIN
            Dim colEIN As New DataGridViewTextBoxColumn
            With colEIN
                .DataPropertyName = "EIN"
                .HeaderText = "EIN"
                .Name = "EIN"
                .Width = 80
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgv_Sched_A_Drop.Columns.Add(colEIN)


            'Set DataGridView textbox Column for NAICCode
            Dim colNAICCode As New DataGridViewTextBoxColumn
            With colNAICCode
                .DataPropertyName = "NAICCode"
                .HeaderText = "NAIC Code"
                .Name = "NAICCode"
                .Width = 60
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgv_Sched_A_Drop.Columns.Add(colNAICCode)

            'Set DataGridView textbox Column for BrokerComm
            Dim colBrokerComm As New DataGridViewTextBoxColumn
            With colBrokerComm
                .DataPropertyName = "BrokerComm"
                .HeaderText = "Broker Comm"
                .Name = "BrokerComm"
                .Width = 85
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "c0"
            End With
            dgv_Sched_A_Drop.Columns.Add(colBrokerComm)

            'Set DataGridView textbox Column for Analysis
            Dim colAnalysis As New DataGridViewTextBoxColumn
            With colAnalysis
                .DataPropertyName = "Analysis"
                .HeaderText = "Analysis"
                .Name = "Analysis"
                .Width = 200
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgv_Sched_A_Drop.Columns.Add(colAnalysis)


        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgv_Sched_A_Drop_FormatGrid", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgv_Sched_A_Drop_FormatGrid " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub dgv_Sched_A_Drop_BindData()
        Try
            dgv_Sched_A_Drop.Rows.Clear()
            For i As Integer = 0 To dsDOLScheduleA_Deletes.Tables(0).Rows.Count - 1
                Me.dgv_Sched_A_Drop.Rows.Add(dsDOLScheduleA_Deletes.Tables(0).Rows(i).Item(0), dsDOLScheduleA_Deletes.Tables(0).Rows(i).Item(1), _
                                    dsDOLScheduleA_Deletes.Tables(0).Rows(i).Item(2), _
                                    dsDOLScheduleA_Deletes.Tables(0).Rows(i).Item(3), dsDOLScheduleA_Deletes.Tables(0).Rows(i).Item(4), _
                                    dsDOLScheduleA_Deletes.Tables(0).Rows(i).Item(5), dsDOLScheduleA_Deletes.Tables(0).Rows(i).Item(6), _
                                    dsDOLScheduleA_Deletes.Tables(0).Rows(i).Item(7), dsDOLScheduleA_Deletes.Tables(0).Rows(i).Item(8))
            Next
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgv_Sched_A_Drop_BindData", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgv_Sched_A_Drop_BindData  : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvSched_C_FormatGrid()
        'This is a general formatting grid subroutine for the HMO, PPO, HMOMedicare, HMOMedicaid datagrids
        Try
            'set Visual Basic Datagrid Header style to false so we can use our own
            'The key statement required to get the column and row styles to work
            'Visual Header styles must be shut off
            dgv_ScheduleC.EnableHeadersVisualStyles = False
            'go and set the styles
            With dgv_ScheduleC
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
            Dim colDOLID As New DataGridViewTextBoxColumn
            With colDOLID
                .DataPropertyName = "DOLID"
                .Name = "DOLID"
                .Width = 55
            End With
            dgv_ScheduleC.Columns.Add(colDOLID)

            'Set DataGridView textbox Column for Row_Number
            Dim colRow_Number As New DataGridViewTextBoxColumn
            With colRow_Number
                .DataPropertyName = "Row_Number"
                .HeaderText = "Row Number"
                .Name = "Row_Number"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .Width = 55
            End With
            dgv_ScheduleC.Columns.Add(colRow_Number)


            'Set DataGridView textbox Column for EIN
            Dim colEIN As New DataGridViewTextBoxColumn
            With colEIN
                .DataPropertyName = "EIN"
                .HeaderText = "EIN"
                .Name = "EIN"
                .Width = 75
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "$#,##0"
            End With
            dgv_ScheduleC.Columns.Add(colEIN)

            'Set DataGridView textbox Column for Business Name
            Dim colAck_ID As New DataGridViewTextBoxColumn
            With colAck_ID
                .DataPropertyName = "Ack_ID"
                .HeaderText = "ACK ID"
                .Name = "Ack_ID"
                .Width = 225
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##,##0"
            End With
            dgv_ScheduleC.Columns.Add(colAck_ID)


            'Set DataGridView textbox Column for Provider_Name
            Dim colProvider_Name As New DataGridViewTextBoxColumn
            With colProvider_Name
                .DataPropertyName = "Provider_Name"
                .HeaderText = "Provider Name"
                .Name = "Provider_Name"
                .Width = 300
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "c0"
            End With
            dgv_ScheduleC.Columns.Add(colProvider_Name)

            'Set DataGridView textbox Column for Provider_EIN
            Dim colProvider_EIN As New DataGridViewTextBoxColumn
            With colProvider_EIN
                .DataPropertyName = "Provider_EIN"
                .HeaderText = "Provider EIN"
                .Name = "Provider_EIN"
                .Width = 75
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgv_ScheduleC.Columns.Add(colProvider_EIN)


            'Set DataGridView textbox Column for Relation
            Dim colRelation As New DataGridViewTextBoxColumn
            With colRelation
                .DataPropertyName = "Relation"
                .HeaderText = "Relation"
                .Name = "Relation"
                .Width = 175
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgv_ScheduleC.Columns.Add(colRelation)

            'Set DataGridView textbox Column for Direct_Comp_Amt
            Dim colDirect_Comp_Amt As New DataGridViewTextBoxColumn
            With colDirect_Comp_Amt
                .DataPropertyName = "Direct_Comp_Amt"
                .HeaderText = "Direct Comp Amt"
                .Name = "Direct_Comp_Amt"
                .Width = 100
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "c2"
            End With
            dgv_ScheduleC.Columns.Add(colDirect_Comp_Amt)

            'Set DataGridView textbox Column for Tot_ind_comp_Amt
            Dim colTot_ind_comp_Amt As New DataGridViewTextBoxColumn
            With colTot_ind_comp_Amt
                .DataPropertyName = "Tot_ind_comp_Amt"
                .HeaderText = "Tot ind comp Amt"
                .Name = "Tot_ind_comp_Amt"
                .Width = 100
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "c2"
            End With
            dgv_ScheduleC.Columns.Add(colTot_ind_comp_Amt)

        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvSchedule_C_FormatGrid", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvSchedule_C_FormatGrid " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvSched_C_BindData()
        Try
            dgv_ScheduleC.Rows.Clear()
            For i As Integer = 0 To dsDOLScheduleC.Tables(0).Rows.Count - 1
                Me.dgv_ScheduleC.Rows.Add(dsDOLScheduleC.Tables(0).Rows(i).Item(0), dsDOLScheduleC.Tables(0).Rows(i).Item(1), _
                                    dsDOLScheduleC.Tables(0).Rows(i).Item(2), _
                                    dsDOLScheduleC.Tables(0).Rows(i).Item(3), dsDOLScheduleC.Tables(0).Rows(i).Item(4), _
                                    dsDOLScheduleC.Tables(0).Rows(i).Item(5), dsDOLScheduleC.Tables(0).Rows(i).Item(6), _
                                    dsDOLScheduleC.Tables(0).Rows(i).Item(7), dsDOLScheduleC.Tables(0).Rows(i).Item(8))
            Next
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvSched_C_BindData", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvSched_C_BindData  : " + ex.Message)
        End Try
    End Sub

    Private Sub dgv_Sched_C_Drop_FormatGrid()
        'This is a general formatting grid subroutine for the HMO, PPO, HMOMedicare, HMOMedicaid datagrids
        Try
            'set Visual Basic Datagrid Header style to false so we can use our own
            'The key statement required to get the column and row styles to work
            'Visual Header styles must be shut off
            dgv_Sched_C_Drop.EnableHeadersVisualStyles = False
            'go and set the styles
            With dgv_Sched_C_Drop
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
                .RowHeadersWidth = 32
            End With
            'Set DataGridView textbox Column for Duns
            Dim colDOLID As New DataGridViewTextBoxColumn
            With colDOLID
                .DataPropertyName = "DOLID"
                .Name = "DOLID"
                .Visible = False
                .Width = 78
            End With
            dgv_Sched_C_Drop.Columns.Add(colDOLID)

            'Set DataGridView textbox Column for ROW_ORDER
            Dim colROW_ORDER As New DataGridViewTextBoxColumn
            With colROW_ORDER
                .DataPropertyName = "ROW_ORDER"
                .HeaderText = "Row Order"
                .Name = "ROW_ORDER"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .Width = 35
            End With
            dgv_Sched_C_Drop.Columns.Add(colROW_ORDER)


            'Set DataGridView textbox Column for EIN
            Dim colEIN As New DataGridViewTextBoxColumn
            With colEIN
                .DataPropertyName = "EIN"
                .HeaderText = "EIN"
                .Name = "EIN"
                .Width = 75
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgv_Sched_C_Drop.Columns.Add(colEIN)

            'Set DataGridView textbox Column for Business Name
            Dim colAck_ID As New DataGridViewTextBoxColumn
            With colAck_ID
                .DataPropertyName = "Ack_ID"
                .HeaderText = "ACK_ID"
                .Name = "Ack_ID"
                .Width = 225
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##,##0"
            End With
            dgv_Sched_C_Drop.Columns.Add(colAck_ID)


            'Set DataGridView textbox Column for Provider_Name
            Dim colProvider_Name As New DataGridViewTextBoxColumn
            With colProvider_Name
                .DataPropertyName = "Provider_Name"
                .HeaderText = "Provider Name"
                .Name = "Provider_Name"
                .Width = 300
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgv_Sched_C_Drop.Columns.Add(colProvider_Name)

            'Set DataGridView textbox Column for Provider_EIN
            Dim colProvider_EIN As New DataGridViewTextBoxColumn
            With colProvider_EIN
                .DataPropertyName = "Provider_EIN"
                .HeaderText = "Provider_EIN"
                .Name = "Provider_EIN"
                .Width = 75
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgv_Sched_C_Drop.Columns.Add(colProvider_EIN)

            'Set DataGridView textbox Column for Relation
            Dim colRelation As New DataGridViewTextBoxColumn
            With colRelation
                .DataPropertyName = "Relation"
                .HeaderText = "Relation"
                .Name = "Relation"
                .Width = 175
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgv_Sched_C_Drop.Columns.Add(colRelation)


            'Set DataGridView textbox Column for Direct_Comp_Amt
            Dim colDirect_Comp_Amt As New DataGridViewTextBoxColumn
            With colDirect_Comp_Amt
                .DataPropertyName = "Direct_Comp_Amt"
                .HeaderText = "Direct Comp Amt"
                .Name = "Direct_Comp_Amt"
                .Width = 95
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "c2"
            End With
            dgv_Sched_C_Drop.Columns.Add(colDirect_Comp_Amt)

            'Set DataGridView textbox Column for Tot_ind_comp_Amt
            Dim colTot_ind_comp_Amt As New DataGridViewTextBoxColumn
            With colTot_ind_comp_Amt
                .DataPropertyName = "Tot_ind_comp_Amt"
                .HeaderText = "Tot ind comp Amt"
                .Name = "Tot_ind_comp_Amt"
                .Width = 95
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                .DefaultCellStyle.Format = "c2"
            End With
            dgv_Sched_C_Drop.Columns.Add(colTot_ind_comp_Amt)

            'Set DataGridView textbox Column for Analysis
            Dim colAnalysis As New DataGridViewTextBoxColumn
            With colAnalysis
                .DataPropertyName = "Analysis"
                .HeaderText = "Analysis"
                .Name = "Analysis"
                .Width = 100
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgv_Sched_C_Drop.Columns.Add(colAnalysis)


        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgv_Sched_C_Drop_FormatGrid", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgv_Sched_C_Drop_FormatGrid  : " + ex.Message)
        End Try
    End Sub

    Private Sub dgv_Sched_C_Drop_BindData()
        Try
            dgv_Sched_C_Drop.Rows.Clear()
            For i As Integer = 0 To dsDOLScheduleC_Deletes.Tables(0).Rows.Count - 1
                Me.dgv_Sched_C_Drop.Rows.Add(dsDOLScheduleC_Deletes.Tables(0).Rows(i).Item(0), dsDOLScheduleC_Deletes.Tables(0).Rows(i).Item(1), _
                                    dsDOLScheduleC_Deletes.Tables(0).Rows(i).Item(2), _
                                    dsDOLScheduleC_Deletes.Tables(0).Rows(i).Item(3), dsDOLScheduleC_Deletes.Tables(0).Rows(i).Item(4), _
                                    dsDOLScheduleC_Deletes.Tables(0).Rows(i).Item(5), dsDOLScheduleC_Deletes.Tables(0).Rows(i).Item(6), _
                                    dsDOLScheduleC_Deletes.Tables(0).Rows(i).Item(7), dsDOLScheduleC_Deletes.Tables(0).Rows(i).Item(8), _
                                    dsDOLScheduleC_Deletes.Tables(0).Rows(i).Item(9))
            Next
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgv_Sched_C_Drop_BindData", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgv_Sched_C_Drop_BindData  : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvDuns_Dln_FormatGrid()
        'This is a general formatting grid subroutine for the HMO, PPO, HMOMedicare, HMOMedicaid datagrids
        Try
            'set Visual Basic Datagrid Header style to false so we can use our own
            'The key statement required to get the column and row styles to work
            'Visual Header styles must be shut off
            dgvDuns_Dln.EnableHeadersVisualStyles = False
            'go and set the styles
            With dgvDuns_Dln
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
                .RowHeadersWidth = 28
            End With
            'Set DataGridView textbox Column for Duns
            Dim colDOLID As New DataGridViewTextBoxColumn
            With colDOLID
                .DataPropertyName = "DOLID"
                .Name = "DOLID"
                .Visible = False
                '.Width = 70
            End With
            dgvDuns_Dln.Columns.Add(colDOLID)

            'Set DataGridView textbox Column for DUNS
            Dim colDUNS As New DataGridViewTextBoxColumn
            With colDUNS
                .DataPropertyName = "DUNS"
                .HeaderText = "DUNS"
                .Name = "DUNS"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .Width = 90
            End With
            dgvDuns_Dln.Columns.Add(colDUNS)


            'Set DataGridView textbox Column for BusinessName
            Dim colBusinessName As New DataGridViewTextBoxColumn
            With colBusinessName
                .DataPropertyName = "BusinessName"
                .HeaderText = "Business Name"
                .Name = "BusinessName"
                .Width = 290
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDuns_Dln.Columns.Add(colBusinessName)

            'Set DataGridView textbox Column for Business Name
            Dim colEIN As New DataGridViewTextBoxColumn
            With colEIN
                .DataPropertyName = "EIN"
                .HeaderText = "EIN"
                .Name = "EIN"
                .Width = 80
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##,##0"
            End With
            dgvDuns_Dln.Columns.Add(colEIN)


            'Set DataGridView textbox Column for PN
            Dim colPN As New DataGridViewTextBoxColumn
            With colPN
                .DataPropertyName = "PN"
                .HeaderText = "PN"
                .Name = "PN"
                .Width = 36
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDuns_Dln.Columns.Add(colPN)

            'Set DataGridView textbox Column for DLN
            Dim colDLN As New DataGridViewTextBoxColumn
            With colDLN
                .DataPropertyName = "DLN"
                .HeaderText = "DLN"
                .Name = "DLN"
                .Width = 225
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDuns_Dln.Columns.Add(colDLN)

            'Set DataGridView textbox Column for PlanName
            Dim colPlanName As New DataGridViewTextBoxColumn
            With colPlanName
                .DataPropertyName = "PlanName"
                .HeaderText = "Plan Name"
                .Name = "PlanName"
                .Width = 445
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgvDuns_Dln.Columns.Add(colPlanName)

            'Set DataGridView textbox Column for Subsidiary DUNS
            Dim colSubsidiaryDUNS As New DataGridViewTextBoxColumn
            With colSubsidiaryDUNS
                .DataPropertyName = "Subsidiary DUNS"
                .HeaderText = "Subsidiary DUNS"
                .Name = "SubsidiaryDUNS"
                .Width = 90
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvDuns_Dln.Columns.Add(colSubsidiaryDUNS)

            'Set DataGridView textbox Column for SubsidiaryEIN
            Dim colSubsidiaryEIN As New DataGridViewTextBoxColumn
            With colSubsidiaryEIN
                .DataPropertyName = "Subsidiary EIN"
                .HeaderText = "Subsidiary EIN"
                .Name = "SubsidiaryEIN"
                .Width = 80
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
            End With
            dgvDuns_Dln.Columns.Add(colSubsidiaryEIN)

        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvDuns_Dln_FormatGrid", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvDuns_Dln_FormatGrid " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvDuns_Dln_BindData()
        Try
            dgvDuns_Dln.Rows.Clear()
            For i As Integer = 0 To dsDuns_Dln.Tables(0).Rows.Count - 1
                Me.dgvDuns_Dln.Rows.Add(dsDuns_Dln.Tables(0).Rows(i).Item(0), dsDuns_Dln.Tables(0).Rows(i).Item(1), _
                                    dsDuns_Dln.Tables(0).Rows(i).Item(2), _
                                    dsDuns_Dln.Tables(0).Rows(i).Item(3), dsDuns_Dln.Tables(0).Rows(i).Item(4), _
                                    dsDuns_Dln.Tables(0).Rows(i).Item(5), dsDuns_Dln.Tables(0).Rows(i).Item(6), _
                                    dsDuns_Dln.Tables(0).Rows(i).Item(7), dsDuns_Dln.Tables(0).Rows(i).Item(8))
            Next
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvDuns_Dln_BindData", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvDuns_Dln_BindData  : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvPBMs_FormatGrid()
        'This is a general formatting grid subroutine for the HMO, PPO, HMOMedicare, HMOMedicaid datagrids
        Try
            'set Visual Basic Datagrid Header style to false so we can use our own
            'The key statement required to get the column and row styles to work
            'Visual Header styles must be shut off
            dgvPBMs.EnableHeadersVisualStyles = False
            'go and set the styles
            With dgvPBMs
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
                .RowHeadersWidth = 50
            End With


            'Set DataGridView textbox Column for PBMCName
            Dim colPBMCName As New DataGridViewTextBoxColumn
            With colPBMCName
                .DataPropertyName = "PBMCName"
                .HeaderText = "PBMC Name"
                .Name = "PBMCName"
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .Width = 175
            End With
            dgvPBMs.Columns.Add(colPBMCName)

            'Set DataGridView textbox Column for Business Name
            Dim colEIN As New DataGridViewTextBoxColumn
            With colEIN
                .DataPropertyName = "EIN"
                .HeaderText = "EIN"
                .Name = "EIN"
                .Width = 90
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##,##0"
            End With
            dgvPBMs.Columns.Add(colEIN)


            'Set DataGridView textbox Column for ExternalPBMCDimID
            Dim colExternalPBMCDimID As New DataGridViewTextBoxColumn
            With colExternalPBMCDimID
                .DataPropertyName = "ExternalPBMCDimID"
                .HeaderText = "External PBMC DimID"
                .Name = "ExternalPBMCDimID"
                .Width = 80
                .DefaultCellStyle.Font = New Font("Arial", 9, FontStyle.Regular)
                .DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
                '.DefaultCellStyle.Format = "##.00"
            End With
            dgvPBMs.Columns.Add(colExternalPBMCDimID)

        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvPBMs_FormatGrid", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvPBMs_FormatGrid " + cmbChangeList.SelectedValue(1) + " : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvPBMs_BindData()
        Try
            dgvPBMs.Rows.Clear()
            For i As Integer = 0 To dsPBMs.Tables(0).Rows.Count - 1
                Me.dgvPBMs.Rows.Add(dsPBMs.Tables(0).Rows(i).Item(0), dsPBMs.Tables(0).Rows(i).Item(1), _
                                    dsPBMs.Tables(0).Rows(i).Item(2))
            Next
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "dgvPBMs_BindData", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvPBMs_BindData  : " + ex.Message)
        End Try
    End Sub

    Private Sub dgvSched_A_UserDeletingRow(ByVal sender As System.Object, e As DataGridViewRowCancelEventArgs) Handles dgvSched_A.UserDeletingRow
        Dim iResult As Integer, sortColumn As DataGridViewColumn, myindex As Integer, mysortcolumn As Integer, iResult2 As Integer
        Dim SetSortOrder As ListSortDirection
        Dim GridSortOrder As SortOrder
        Try
            If Not bInitial And Not boolReadOnly Then
                bInitial = True
                If dgvSched_A.SelectedRows.Count > 1 Then
                    MsgBox("please only select 1 row to delete at a time")
                Else

                    sortColumn = dgvSched_A.SortedColumn

                    If Not sortColumn Is Nothing Then
                        mysortcolumn = sortColumn.Index
                        GridSortOrder = dgvSched_A.SortOrder
                    End If

                    myindex = dgvSched_A.CurrentRow.Index

                    iResult = MsgBox("Do you wish to make this a Schedule C record?  Press 'Yes' to do so, 'No' to Delete or 'Cancel' to return", MsgBoxStyle.YesNoCancel)

                    e.Cancel = True

                    If iResult = 6 Then
                        iResult2 = SQLHelper.ExecuteScalar(CN, "Emp.s_MoveScheduleAtoC", _
                                                      dgvSched_A.Rows(dgvSched_A.CurrentRow.Index).Cells("DOLID").Value.ToString, _
                                                      dgvSched_A.Rows(dgvSched_A.CurrentRow.Index).Cells("Form_ID").Value.ToString, _
                                                      Userid)
                        If iResult2 = 0 Then
                            MsgBox("Record movved")
                            Me.Cursor = Cursors.AppStarting

                            dsDOLScheduleA = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_A")
                            dgvSched_A_BindData()

                            dsDOLScheduleC = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_C")
                            dgvSched_C_BindData()
                        Else
                            MsgBox("Move failed")
                        End If
                    ElseIf iResult = 2 Then
                        'just mark it as delete.
                        iResult2 = SQLHelper.ExecuteScalar(CN, "Emp.s_UserDelete_ScheduleA", _
                                                      dgvSched_A.Rows(dgvSched_A.CurrentRow.Index).Cells("DOLID").Value.ToString, _
                                                      dgvSched_A.Rows(dgvSched_A.CurrentRow.Index).Cells("Form_ID").Value.ToString, _
                                                      Userid)
                        If iResult2 = 0 Then
                            MsgBox("Record marked for deletion")
                        Else
                            MsgBox("Delete failed")
                        End If
                    End If ' no need to do anything special for 'No'



                    Me.Cursor = Cursors.AppStarting

                    dsDOLScheduleA = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_A")
                    dgvSched_A_BindData()

                    dsDOLScheduleA_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_A")
                    dgv_Sched_A_Drop_BindData()

                    If GridSortOrder = Windows.Forms.SortOrder.Ascending Then
                        SetSortOrder = ListSortDirection.Ascending
                    ElseIf GridSortOrder = Windows.Forms.SortOrder.Descending Then
                        SetSortOrder = ListSortDirection.Descending
                    ElseIf GridSortOrder = Windows.Forms.SortOrder.None Then
                        SetSortOrder = ListSortDirection.Ascending
                    Else : GridSortOrder = ListSortDirection.Ascending
                        MsgBox("not good")
                    End If

                    If Not sortColumn Is Nothing Then
                        dgvSched_A.Sort(sortColumn, SetSortOrder)
                        Me.dgvSched_A.CurrentCell = Me.dgvSched_A(mysortcolumn, myindex)
                    End If

                    Me.Cursor = Cursors.Default
                    bInitial = False
                End If
            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "dgvSched_A_UserDeletingRow ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgvSched_A_UserDeletingRow : " + ex.Message)
        End Try
    End Sub

    Private Sub dgv_Sched_A_Drop_UserDeletingRow(ByVal sender As System.Object, e As DataGridViewRowCancelEventArgs) Handles dgv_Sched_A_Drop.UserDeletingRow
        Dim iResult As Integer, sortColumn As DataGridViewColumn, myindex As Integer, mysortcolumn As Integer
        Dim SetSortOrder As ListSortDirection
        Dim GridSortOrder As SortOrder

        Try
            e.Cancel = True ' never actually delete the row.
            If Not bInitial And Not boolReadOnly Then
                bInitial = True
                If dgv_Sched_A_Drop.SelectedRows.Count > 1 Then
                    MsgBox("please only select 1 row to delete at a time")
                Else

                    sortColumn = dgv_Sched_A_Drop.SortedColumn

                    If Not sortColumn Is Nothing Then
                        mysortcolumn = sortColumn.Index
                        GridSortOrder = dgv_Sched_A_Drop.SortOrder
                    End If

                    myindex = dgv_Sched_A_Drop.CurrentRow.Index

                    iResult = SQLHelper.ExecuteScalar(CN, "Emp.s_User_UnDelete_ScheduleA", _
                                                      dgv_Sched_A_Drop.Rows(dgv_Sched_A_Drop.CurrentRow.Index).Cells("DOLID").Value.ToString, _
                                                      dgv_Sched_A_Drop.Rows(dgv_Sched_A_Drop.CurrentRow.Index).Cells("Form_ID").Value.ToString, _
                                                      Userid)
                    If iResult = 0 Then
                        MsgBox("Record unmarked for deletion")
                    Else
                        MsgBox("Undelete failed")
                    End If


                    Me.Cursor = Cursors.AppStarting

                    dsDOLScheduleA = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_A")
                    dgvSched_A_BindData()

                    dsDOLScheduleA_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_A")
                    dgv_Sched_A_Drop_BindData()

                    If GridSortOrder = Windows.Forms.SortOrder.Ascending Then
                        SetSortOrder = ListSortDirection.Ascending
                    ElseIf GridSortOrder = Windows.Forms.SortOrder.Descending Then
                        SetSortOrder = ListSortDirection.Descending
                    ElseIf GridSortOrder = Windows.Forms.SortOrder.None Then
                        SetSortOrder = ListSortDirection.Ascending
                    Else : GridSortOrder = ListSortDirection.Ascending
                        MsgBox("not good")
                    End If

                    If Not sortColumn Is Nothing Then
                        dgv_Sched_A_Drop.Sort(sortColumn, SetSortOrder)
                        Me.dgv_Sched_A_Drop.CurrentCell = Me.dgv_Sched_A_Drop(mysortcolumn, myindex)
                    End If
                    Me.Cursor = Cursors.Default
                    bInitial = False
                End If

            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "dgv_Sched_A_Drop_UserDeletingRow ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgv_Sched_A_Drop_UserDeletingRow : " + ex.Message)
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
            ElseIf radSubDelete.Checked And ckbSortAlpha.Checked And ckbExpandedList.Checked Then
                dsSubDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_SubDelete_List", 1, 50)
            ElseIf radSubDelete.Checked And ckbSortAlpha.Checked And Not ckbExpandedList.Checked Then
                dsSubDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_SubDelete_List", 1, 500)
            ElseIf radSubDelete.Checked Then
                dsSubDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_SubDelete_List", 0, 500)
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

    Private Sub ckbSortAlpha_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles ckbSortAlpha.CheckedChanged
        Reprocess_Sub_List()
    End Sub

    Private Sub LinkLabel3_LinkClicked(sender As System.Object, e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles LinkLabel3.LinkClicked
        System.Diagnostics.Process.Start("http://myreports/DRG/Pages/Report.aspx?ItemPath=%2fProd%2fEV+Validation%2fCorporations+that+ALL+Subsidiaries+were+marked+for+Delete+(not+found+in+the+incoming+list)")
    End Sub

    Private Sub ckbExpandedList_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles ckbExpandedList.CheckedChanged
        Reprocess_Sub_List()
    End Sub

    Private Sub get_DOL_CleanStats()
        dsDOLCleanStats = SQLHelper.ExecuteDataset(CN, "EMP.s_get_DOLCleanStats")

        If dsDOLCleanStats.Tables(0).Rows.Count > 0 Then
            Label96.Text = dsDOLCleanStats.Tables(0).Rows(0).Item("Clean") + " Clean"
            Label97.Text = dsDOLCleanStats.Tables(0).Rows(0).Item("Participants") + " < 250 Participants"
            Label98.Text = dsDOLCleanStats.Tables(0).Rows(0).Item("WelfareBenefit") + " Welfare Benefit Code"
            Label99.Text = dsDOLCleanStats.Tables(0).Rows(0).Item("PlanYear") + " Plan Year"
            Label100.Text = dsDOLCleanStats.Tables(0).Rows(0).Item("Injury") + " Injury"
            Label101.Text = dsDOLCleanStats.Tables(0).Rows(0).Item("FlexPlans") + " Flex/Reimbursement"
        End If
    End Sub

    Private Sub ckb5KLimit_CheckedChanged(sender As System.Object, e As System.EventArgs) Handles ckb5KLimit.CheckedChanged
        Try
            If Not bInitial Then
                bInitial = True

                If ckb5KLimit.Checked Then
                    b5KLimit = True
                Else
                    b5KLimit = False
                End If
                dsDOLF5500 = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_5500", b5KLimit)
                dgvClean5500_BindData()

                dsDOLF5500Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_5500", b5KLimit)
                dgvDirty5500_BindData()

                bInitial = False
            End If
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "ckb5KLimit_CheckedChanged ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : ckb5KLimit_CheckedChanged : " + ex.Message)
        End Try
    End Sub

    Private Sub RUN_DTSX_Package(ByVal Package As String)
        Try
            Dim jobConnection As SqlConnection
            Dim jobCommand As SqlCommand
            Dim jobReturnValue As SqlParameter
            Dim jobParameter As SqlParameter
            Dim jobResult As Integer


            jobConnection = New SqlConnection("Data Source=NASPROSQL1;Initial Catalog=msdb;User ID=HLIDBAdmin;PWD=^HLI<&dm!n")
            jobCommand = New SqlCommand("sp_start_job", jobConnection)
            jobCommand.CommandType = CommandType.StoredProcedure

            'required
            jobReturnValue = New SqlParameter("@RETURN_VALUE", SqlDbType.Int)
            jobReturnValue.Direction = ParameterDirection.ReturnValue
            jobCommand.Parameters.Add(jobReturnValue)

            'required
            jobParameter = New SqlParameter("@job_name", SqlDbType.VarChar)
            jobParameter.Direction = ParameterDirection.Input
            jobCommand.Parameters.Add(jobParameter)
            jobParameter.Value = Package   'This is the DTSX package that exists in the directory of the Executable

            jobConnection.Open()
            jobCommand.ExecuteNonQuery()
            jobResult = DirectCast(jobCommand.Parameters("@RETURN_VALUE").Value, Integer)
            jobConnection.Close()

        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub


    Private Sub btnReAssess_Click(sender As System.Object, e As System.EventArgs) Handles btnReAssess.Click
        Dim dsfix As New DataSet


        Try
            If Not boolReadOnly Then
                Me.Cursor = Cursors.AppStarting

                dsfix = SQLHelper.ExecuteDataset(CN, "Emp.S_Reassess_DUNS_DLN")

                MsgBox("No records Updated")
               

                dsDuns_Dln = SQLHelper.ExecuteDataset(CN, "emp.s_get_Duns_Dln")
                dgvDuns_Dln_BindData()

                Me.Cursor = Cursors.Default
            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnReAssess_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnReAssess_Click : " + ex.Message)
        End Try
    End Sub


  
    Private Sub btnAcceptCorporatesDeletes_Click(sender As System.Object, e As System.EventArgs) Handles btnAcceptCorporatesDeletes.Click
        Dim iresult As Integer
        Try

            Me.Cursor = Cursors.AppStarting
            dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Delete_List")

            While dsDemotionList.Tables(0).Rows.Count > 1
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_Delete", dsDemotionList.Tables(0).Rows(1).Item("EmployerID"), strCurrentUser)
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_Delete " + CStr(dsDemotionList.Tables(0).Rows(1).Item("EmployerID")) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using

                dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Delete_List")
            End While

            MsgBox("Deletion Complete")
            Me.Cursor = Cursors.Default
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnAcceptCorporatesDeletes_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAcceptCorporatesDeletes_Click : " + ex.Message)
        End Try

    End Sub

    Private Sub btnAcceptAllAdds_Click(sender As System.Object, e As System.EventArgs) Handles btnAcceptAllAdds.Click
        Dim iresult As Integer
        Try

            Me.Cursor = Cursors.AppStarting
            dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Addition_List")

            While dsDemotionList.Tables(0).Rows.Count > 1
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_Addition", dsDemotionList.Tables(0).Rows(1).Item("EmployerID"), strCurrentUser)
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_Addition " + CStr(dsDemotionList.Tables(0).Rows(1).Item("EmployerID")) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using

                dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Addition_List")
            End While

            MsgBox("Additions Complete")
            Me.Cursor = Cursors.Default
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnAcceptAllAdds_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAcceptAllAdds_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnAcceptCorporateDemotion_Click(sender As System.Object, e As System.EventArgs) Handles btnAcceptCorporateDemotion.Click
        Dim iresult As Integer
        Try

            Me.Cursor = Cursors.AppStarting
            dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Demotion_List")

            While dsDemotionList.Tables(0).Rows.Count > 1
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_Demotion", dsDemotionList.Tables(0).Rows(1).Item("EmployerID"), strCurrentUser)
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_Demotion " + CStr(dsDemotionList.Tables(0).Rows(1).Item("EmployerID")) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using

                dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Demotion_List")
            End While

            MsgBox("Demotions Complete")
            Me.Cursor = Cursors.Default
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnAcceptCorporateDemotion_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAcceptCorporateDemotion_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub btnAcceptBulkPromotion_Click(sender As System.Object, e As System.EventArgs) Handles btnAcceptBulkPromotion.Click
        Dim iresult As Integer
        Try

            Me.Cursor = Cursors.AppStarting
            dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Promotion_List")

            While dsDemotionList.Tables(0).Rows.Count > 1
                iresult = SQLHelper.ExecuteScalar(CN, "EMP.s_Accept_Promotion", dsDemotionList.Tables(0).Rows(1).Item("EmployerID"), strCurrentUser)
                Using sw As StreamWriter = File.AppendText(path)
                    sw.WriteLine("EMP.s_Accept_Promotion " + CStr(dsDemotionList.Tables(0).Rows(1).Item("EmployerID")) + ", " + strCurrentUser)
                    sw.WriteLine("Go")
                End Using

                dsDemotionList = GlobalLibrary.SqlHelper.ExecuteDataset(CN, "EMP.s_Get_Promotion_List")
            End While

            MsgBox("Promotions Complete")
            Me.Cursor = Cursors.Default
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "btnAcceptBulkPromotion_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnAcceptBulkPromotion_Click : " + ex.Message)
        End Try
    End Sub

    Private Sub LinkLabel4_LinkClicked(sender As System.Object, e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles LinkLabel4.LinkClicked
        System.Diagnostics.Process.Start("http://myreports/DRG/Pages/Report.aspx?ItemPath=%2fProd%2fEV+Validation%2fCorporate+records+that+do+not+have+an+associated+Form+5500&SelectedSubTabId=ReportDataSourcePropertiesTab")
    End Sub

    Private Sub LinkLabel5_LinkClicked(sender As System.Object, e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles LinkLabel5.LinkClicked
        System.Diagnostics.Process.Start("http://myreports/DRG/Pages/Report.aspx?ItemPath=%2fProd%2fEV+Validation%2fList+of+possible+EIN+matches")
    End Sub

    Private Sub LinkLabel6_LinkClicked(sender As System.Object, e As System.Windows.Forms.LinkLabelLinkClickedEventArgs) Handles LinkLabel6.LinkClicked
        System.Diagnostics.Process.Start("http://myreports/DRG/Pages/Report.aspx?ItemPath=%2fProd%2fEV+Validation%2fPossible+Corporate+to+5500+Match+based+on+Partial+Name+Match")
    End Sub

    Private Sub btnDeleteMarkedCs_Click(sender As System.Object, e As System.EventArgs) Handles btnDeleteMarkedCs.Click
        Dim iresult As Integer

        Try
            If Not boolReadOnly Then
                iresult = MsgBox("This will delete all of the Schedule Cs that are marked for deletion.  Are you sure you want to do this?", MsgBoxStyle.YesNo)
                If iresult = 6 Then
                    Me.Cursor = Cursors.AppStarting
                    Using conn As New SqlClient.SqlConnection(CN)
                        conn.Open()
                        Using cm As New SqlClient.SqlCommand("emp.s_Delete_ScheduleC", conn)
                            cm.CommandType = CommandType.StoredProcedure
                            cm.CommandTimeout = 500
                            cm.ExecuteNonQuery()
                        End Using
                    End Using

                    dsDOLScheduleC = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_C")
                    dgvSched_C_BindData()

                    dsDOLScheduleC_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_C")
                    dgv_Sched_C_Drop_BindData()

                    Me.Cursor = Cursors.Default
                    'If dsDOLScheduleC.Tables.Count = 0 Then
                    '    MsgBox("No records fixed")
                    'Else
                    '    MsgBox(CStr(dsDOLScheduleC.Tables(0).Rows(0).Item(0).ToString) + " records fixed.")
                    'End If
                End If
            End If
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "btnDeleteMarkedCs_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnDeleteMarkedCs_Click : " + ex.Message)
            Me.Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub dgv_ScheduleC_UserDeletedRow(ByVal sender As System.Object, e As DataGridViewRowCancelEventArgs) Handles dgv_ScheduleC.UserDeletingRow
        Dim iResult As Integer, sortColumn As DataGridViewColumn, myindex As Integer, mysortcolumn As Integer
        Dim SetSortOrder As ListSortDirection
        Dim GridSortOrder As SortOrder

        Try
            e.Cancel = True ' never actually delete the row.
            If Not bInitial And Not boolReadOnly Then
                bInitial = True
                If dgv_ScheduleC.SelectedRows.Count > 1 Then
                    MsgBox("please only select 1 row to delete at a time")

                Else

                    sortColumn = dgv_ScheduleC.SortedColumn


                    If Not sortColumn Is Nothing Then
                        mysortcolumn = sortColumn.Index
                        GridSortOrder = dgv_ScheduleC.SortOrder
                    End If

                    myindex = dgv_ScheduleC.CurrentRow.Index
                    MsgBox("Deleteing " + dgv_ScheduleC.SelectedRows(0).Cells("DOLID").Value.ToString + "  " + dgv_ScheduleC.SelectedRows(0).Cells("Row_Number").Value.ToString)

                    iResult = SQLHelper.ExecuteScalar(CN, "Emp.s_UserDelete_ScheduleC", _
                                                      dgv_ScheduleC.SelectedRows(0).Cells("DOLID").Value.ToString, _
                                                      dgv_ScheduleC.SelectedRows(0).Cells("Row_Number").Value.ToString,
                                                      Userid)
                    If iResult = 0 Then
                        MsgBox("Record marked for deletion")
                    Else
                        MsgBox("Delete failed")
                    End If


                    Me.Cursor = Cursors.AppStarting

                    dsDOLScheduleC = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_C")
                    dgvSched_C_BindData()

                    dsDOLScheduleC_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_C")
                    dgv_Sched_C_Drop_BindData()


                    If GridSortOrder = Windows.Forms.SortOrder.Ascending Then
                        SetSortOrder = ListSortDirection.Ascending
                    ElseIf GridSortOrder = Windows.Forms.SortOrder.Descending Then
                        SetSortOrder = ListSortDirection.Descending
                    ElseIf GridSortOrder = Windows.Forms.SortOrder.None Then
                        SetSortOrder = ListSortDirection.Ascending
                    Else : GridSortOrder = ListSortDirection.Ascending
                        MsgBox("not good")
                    End If

                    If Not sortColumn Is Nothing Then
                        dgv_ScheduleC.Sort(sortColumn, SetSortOrder)
                        Me.dgv_ScheduleC.CurrentCell = Me.dgv_ScheduleC(mysortcolumn, myindex)
                    End If

                    Me.Cursor = Cursors.Default
                    bInitial = False
                End If
            End If
        Catch ex As Exception
            Me.Cursor = Cursors.Default
            'Functions.Sendmail(ex.Message, "dgv_ScheduleC_UserDeletedRow ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : dgv_ScheduleC_UserDeletedRow : " + ex.Message)
        End Try
    End Sub

    Private Sub btnDelete_As_Click(sender As System.Object, e As System.EventArgs) Handles btnDelete_As.Click
        Dim iresult As Integer

        Try
            If Not boolReadOnly Then
                iresult = MsgBox("This will delete all of the Schedule As that are marked for deletion.  Are you sure you want to do this?", MsgBoxStyle.YesNo)
                If iresult = 6 Then
                    Me.Cursor = Cursors.AppStarting
                    Using conn As New SqlClient.SqlConnection(CN)
                        conn.Open()
                        Using cm As New SqlClient.SqlCommand("emp.s_Delete_ScheduleA", conn)
                            cm.CommandType = CommandType.StoredProcedure
                            cm.CommandTimeout = 500
                            cm.ExecuteNonQuery()
                        End Using
                    End Using

                    dsDOLScheduleA = SQLHelper.ExecuteDataset(CN, "emp.s_get_Clean_Sched_A")
                    dgvSched_A_BindData()

                    dsDOLScheduleA_Deletes = SQLHelper.ExecuteDataset(CN, "emp.s_get_dirty_Schedule_A")
                    dgv_Sched_A_Drop_BindData()

                    Me.Cursor = Cursors.Default
                 
                End If
            End If
        Catch ex As Exception
            'Functions.Sendmail(ex.Message, "btnDeleteMarkedCs_Click ", 0, 0, "Employer Maintenance")
            MsgBox("Employer Maintenance : btnDeleteMarkedCs_Click : " + ex.Message)
            Me.Cursor = Cursors.Default
        End Try
    End Sub
End Class
