<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Test.aspx.cs" Inherits="Conquest.Test" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <h1><%=Points %> (lvl <%=Level %>)</h1>

        <p>
            <h2>You just got: 
            <% 
                foreach(var v in YouJustGot)
                {
                    Response.Write(v.Key + "(" + v.Value.ToString() + "), " );
                }
            %></h2>
        </p>

        <ul>
        <% foreach(var m in Overview) { %>
            <li><%=m.TypeKey %> (<%=m.Amount %>)</li>
        <% } %>
        </ul>
    </div>
    </form>
</body>
</html>
