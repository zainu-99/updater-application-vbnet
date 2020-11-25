Imports System.IO
Imports System.Net
Imports System.Windows
Imports Newtonsoft.Json.Linq
Imports Microsoft.SharePoint.Client
Public Class FormUpdater
    Dim LinkAddress = "http://localhost/MIRA/"
    Dim AppName = "MYAPP"
    Dim AppPath As String = Forms.Application.StartupPath()
    Dim EnableDowngrade = False
    Dim client As New WebClient
    Private Sub FormUpdater_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        If My.Settings.noupdate Then
            My.Settings.noupdate = False
            My.Settings.Save()
            Close()
        End If
        If System.IO.File.Exists(AppPath & "\UpdaterSetting.json") Then
            Try
                Dim strJson = Replace(System.IO.File.ReadAllText(AppPath & "\UpdaterSetting.json"), "\", "\\")
                Dim jOnject As JObject = JObject.Parse(strJson)
                LinkAddress = jOnject.GetValue("LinkAddress")
                AppName = jOnject.GetValue("AppName")
                EnableDowngrade = jOnject.GetValue("EnableDowngrade")
                ObjWritter(AppPath & "\UpdaterSetting.json")
            Catch ex As Exception
                MsgBox("")
            End Try
        Else
            Dim FileName As String = AppPath & "\UpdaterSetting.json"
            System.IO.File.Create(FileName).Dispose()
            ObjWritter(FileName)
        End If
        ProgressBarLoading.Maximum = System.IO.Directory.GetFiles(LinkAddress, "*", System.IO.SearchOption.AllDirectories).Count
        System.Threading.ThreadPool.QueueUserWorkItem(AddressOf UpdateExe)
    End Sub
    Sub ObjWritter(FileName As String)
        Dim objWriter As New StreamWriter(FileName, False)
        objWriter.WriteLine("{")
        objWriter.WriteLine("""LinkAddress"" : " & """" & LinkAddress & """,")
        objWriter.WriteLine("""AppName"" : " & """" & AppName & """,")
        objWriter.WriteLine("""EnableDowngrade"" : " & EnableDowngrade.ToString.ToLower & ",")
        objWriter.WriteLine("""Version"" : " & GetCUrVersion.ToString & "")
        objWriter.WriteLine("}")
        objWriter.Close()
    End Sub
    Sub UpdateExe()
        Try
            Forms.Application.DoEvents()
            Dim newVersion As Version = GetFileVersionInfo(LinkAddress & "\" & AppName & ".exe")
            '--------------------------------------------
            Dim AppVersion As Version = GetCUrVersion()
            '---------------------------------------
            Dim cek = newVersion.CompareTo(AppVersion)
            If newVersion.CompareTo(AppVersion) > 0 Or (EnableDowngrade And newVersion.ToString <> AppVersion.ToString) Then
                For Each p As Process In Process.GetProcesses
                    Forms.Application.DoEvents()
                    If p.ProcessName = AppName Then
                        p.Kill()
                    End If
                Next
                Dim result As Integer = MessageBox.Show("Do you want to update " & AppName, "The Latest Version Found", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                If result = DialogResult.No Then
                    My.Settings.noupdate = True
                    My.Settings.Save()
                    Process.Start(AppName)
                    CloseForm()
                    Exit Sub
                End If

                DownloadAllFile(New DirectoryInfo(LinkAddress & "\"), AppPath)
                client.Dispose()
                AppVersion = GetFileVersionInfo(AppPath & "\" & AppName & ".exe")
                If AppVersion.ToString = newVersion.ToString Then
                    Process.Start(AppName)
                Else
                    MsgBox("Cannot Update")
                End If
            End If
        Catch ex As Exception
            MsgBox(ex.Message.ToString)
        End Try
        CloseForm()
    End Sub

    Function GetCUrVersion() As Version
        If IO.File.Exists(AppPath & "\" & AppName & ".exe") Then
            Return GetFileVersionInfo(AppPath & "\" & AppName & ".exe")
        Else
            Return New Version("0.0.0.0")
        End If
    End Function

    Sub CloseForm()
        If InvokeRequired Then
            Invoke(New Action(AddressOf CloseForm))
        Else
            Close()
        End If
    End Sub
    Sub SetMaxProgressBar(max As Int32)
        If InvokeRequired Then
            Invoke(New Action(Of Int32)(Sub() SetMaxProgressBar(max)))
        Else
            Forms.Application.DoEvents()
            Close()
        End If
    End Sub
    Sub SetValueProgressBar(val As Int32)
        If InvokeRequired Then
            Invoke(New Action(Of Int32)(Sub() SetValueProgressBar(val)))
        Else
            Forms.Application.DoEvents()
            Close()
        End If
    End Sub
    Sub DownloadAllFile(ByVal di As DirectoryInfo, dir As String)
        Dim ctr = 0
        For Each fi As FileSystemInfo In di.EnumerateFileSystemInfos()
            ctr += 1
            Try
                ProgressBarLoading.Value = ctr
            Catch ex As Exception

            End Try
            Forms.Application.DoEvents()
            If (fi.Attributes) = FileAttributes.Directory Then
                Try
                    If Not IO.Directory.Exists(dir & "\" & fi.Name) Then
                        IO.Directory.CreateDirectory(dir & "\" & fi.Name)
                    End If
                    DownloadAllFile(New DirectoryInfo(fi.FullName.ToString), dir & "\" & fi.Name)
                Catch ex As Exception
                    MsgBox(ex.Message)
                End Try
            Else
                If Path.GetFileName(fi.Name.ToString) <> "Updater.exe" Then
                    Try
                        If IO.File.Exists(dir & "\" & Path.GetFileName(fi.FullName.ToString)) Then
                            IO.File.Delete(dir & "\" & Path.GetFileName(fi.FullName.ToString))
                        End If
                    Catch ex As Exception
                        My.Application.Log.WriteEntry(ex.Message.ToString)
                    End Try
                    Try
                        client.DownloadFile(fi.FullName.ToString, dir & "\" & Path.GetFileName(fi.FullName.ToString))
                    Catch ex As Exception
                        My.Application.Log.WriteEntry(ex.Message.ToString)
                    End Try
                End If
            End If
        Next
    End Sub

    Private Function GetFileVersionInfo(ByVal filename As String) As Version
        Try
            Return Version.Parse(FileVersionInfo.GetVersionInfo(filename).FileVersion)
        Catch ex As Exception
            Try
                Dim strJson = Replace(IO.File.ReadAllText(AppPath & "\UpdaterSetting.json"), "\", "\\")
                Dim jOnject As JObject = JObject.Parse(strJson)
                Return New Version(jOnject.GetValue("Version").ToString)
            Catch exc As Exception
                Return New Version("0.0.0.0")
            End Try
        End Try
    End Function

End Class
