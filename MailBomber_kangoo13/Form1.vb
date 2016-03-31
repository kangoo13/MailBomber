Imports System.Threading

Public Class Form1
    Dim MailList As New List(Of Mail)()
    Dim AccountList() As String
    Dim ProxiesList() As String
    Dim BodyList() As String
    Dim TitleList() As String
    Dim NamesList() As String
    Dim AccountIndex As Integer
    Dim MailSent As Integer
    Dim SendTo As String
    Dim random As New Random()

    Delegate Sub RefreshStatus([status] As String, [object] As Mail)

    Delegate Sub AddStatus([object] As Mail)

    Private Sub PrepareBody()
        BodyList = IO.File.ReadAllLines(Application.StartupPath + "/body")
    End Sub

    Private Sub PrepareTitle()
        TitleList = IO.File.ReadAllLines(Application.StartupPath + "/titles")
    End Sub

    Private Sub PrepareNames()
        NamesList = IO.File.ReadAllLines(Application.StartupPath + "/names")
    End Sub

    Private Function getRandomNames()
        Return NamesList(random.Next(0, NamesList.Length - 1))
    End Function

    Private Function getRandomTitle()
        Return TitleList(random.Next(0, TitleList.Length - 1))
    End Function

    Private Function getRandomBody()
        Dim body As String = ""

        body += BodyList(random.Next(0, BodyList.Length - 1)) + "<br />"
        body += BodyList(random.Next(0, BodyList.Length - 1)) + "<br />"
        body += BodyList(random.Next(0, BodyList.Length - 1))
        Return body
    End Function

    Private Sub SendMail(ByVal mailObj As Object)
        Dim mailItem As Mail = CType(mailObj, Mail)
        Dim mailman As New Chilkat.MailMan()
        Dim Status As String = ""
        If (ListView1.InvokeRequired) Then
            ListView1.Invoke(New AddStatus(AddressOf AddText), mailItem)
        End If

        '  Any string argument automatically begins the 30-day trial.
        Dim success As Boolean
        success = mailman.UnlockComponent("30-day trial")
        If (success <> True) Then
            Status = "Component unlock failed"
        End If

        '  To connect through an HTTP proxy, set the HttpProxyHostname
        '  and HttpProxyPort properties to the hostname (or IP address)
        '  and port of the HTTP proxy.  Typical port numbers used by
        '  HTTP proxy servers are 3128 and 8080.
        mailman.SocksHostname = mailItem.ipProxy
        mailman.SocksPort = mailItem.portProxy
        mailman.SocksVersion = mailItem.socksType
        mailman.SmtpPort = 465
        mailman.StartTLS = True
        mailman.SmtpSsl = False

        '  Important:  Your HTTP proxy server must allow non-HTTP
        '  traffic to pass.  Otherwise this does not work.

        '  Set the SMTP server.
        mailman.SmtpHost = "smtp.mail.ru"

        '  Set the SMTP login/password (if required)
        mailman.SmtpUsername = mailItem.email
        mailman.SmtpPassword = mailItem.password

        '  Create a new email object
        Dim email As New Chilkat.Email()
        email.SetHtmlBody(mailItem.body)
        email.Subject = mailItem.title
        email.From = mailItem.name + " <" + mailItem.email + ">"
        email.AddTo(SendTo, SendTo)

        '  Call SendEmail to connect to the SMTP server via the HTTP proxy and send.
        '  The connection (i.e. session) to the SMTP server remains
        '  open so that subsequent SendEmail calls may use the
        '  same connection.
        If MailSent <= 399 Then
            success = mailman.SendEmail(email)
        Else
            success = True
            Status = "Max emails sent"
        End If
        If (success <> True) Then
            Status = "Error(spam?proxyBL?)"
        End If


        '  Some SMTP servers do not actually send the email until
        '  the connection is closed.  In these cases, it is necessary to
        '  call CloseSmtpConnection for the mail to be  sent.
        '  Most SMTP servers send the email immediately, and it is
        '  not required to close the connection.  We'll close it here
        '  for the example:
        success = mailman.CloseSmtpConnection()
        If (success <> True) Then
            Status = "Connection to SMTP server not closed cleanly."
        End If
        If Status = "" Then
            Status = "Mail Sent!"
        End If
        If (ListView1.InvokeRequired) Then
            ListView1.Invoke(New RefreshStatus(AddressOf RefreshText), Status, mailItem)
        End If
    End Sub

    Private Sub RefreshText(ByVal [text] As String, ByVal [object] As Mail)
        Dim mailItem As Mail = CType([object], Mail)
        mailItem.StatusObj.Text = [text]
        If [text] = "Mail Sent!" Then
            Label5.Text = Convert.ToString(Convert.ToInt32(Label5.Text) + 1)
            MailSent += 1
        Else
            Label9.Text = Convert.ToString(Convert.ToInt32(Label9.Text) + 1)
        End If
    End Sub

    Private Sub AddText(ByVal [object] As Mail)
        Dim mailItem As Mail = CType([object], Mail)
        Dim ListViewItm As ListViewItem = ListView1.Items.Add(mailItem.email)
        ListViewItm.SubItems.Add(mailItem.password)
        ListViewItm.SubItems.Add(mailItem.proxy)
        Dim obj As ListViewItem.ListViewSubItem = ListViewItm.SubItems.Add("Processing...")
        mailItem.StatusObj = obj
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        SendTo = TextBox1.Text
        For Each mail As Mail In MailList
            ThreadPool.QueueUserWorkItem(AddressOf SendMail, mail)
        Next
    End Sub

    Private Sub ExitToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExitToolStripMenuItem.Click
        Close()
    End Sub

    Private Sub AboutToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AboutToolStripMenuItem.Click
        MsgBox("Contact skype : kangoo131313", MsgBoxStyle.Information, "About")
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            PrepareBody()
            PrepareNames()
            PrepareTitle()
            AccountIndex = 0
            MailSent = 0
            AccountList = IO.File.ReadAllLines(Application.StartupPath + "/accounts")
            ProxiesList = IO.File.ReadAllLines(Application.StartupPath + "/proxies")
            Console.WriteLine(ProxiesList.Length)
            Label1.Text = Str(AccountList.Length) + " accounts loaded"
            Label2.Text = Str(ProxiesList.Length) + " proxies loaded"
            Dim ProxyIndex As Integer = 0
            For Each account As String In AccountList
                Dim mail As New Mail
                Dim credentials() As String = account.Split(":")
                mail.email = credentials(0)
                mail.password = credentials(1)
                If ProxiesList.Length <> 0 Then
                    Dim proxy() As String = ProxiesList(ProxyIndex).Split(":")
                    mail.ipProxy = proxy(0)
                    Dim port As Integer = Convert.ToInt32(proxy(1))
                    mail.portProxy = port
                    mail.socksType = 5
                    mail.proxy = ProxiesList(ProxyIndex)
                    ProxyIndex += 1
                    If ProxyIndex = ProxiesList.Length Then
                        ProxyIndex = 0
                    End If
                End If
                mail.body = getRandomBody()
                mail.title = getRandomTitle()
                mail.name = getRandomNames()
                MailList.Add(mail)
            Next
        Catch ex As Exception
            MsgBox(ex.Message)
        End Try
    End Sub
End Class
