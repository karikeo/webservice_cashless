<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="webservice.aspx.cs" Inherits="WebServiceProject.webservice" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div style="text-align: center">
    
        <asp:GridView ID="GridView1" runat="server" AutoGenerateColumns="False" CellPadding="4" DataKeyNames="userid" DataSourceID="SqlDataSource1" ForeColor="#333333" GridLines="None" style="text-align: center">
            <AlternatingRowStyle BackColor="White" />
            <Columns>
                <asp:BoundField DataField="userid" HeaderText="userid" ReadOnly="True" SortExpression="userid" />
                <asp:BoundField DataField="nombre" HeaderText="nombre" SortExpression="nombre" />
                <asp:BoundField DataField="rut" HeaderText="rut" SortExpression="rut" />
                <asp:BoundField DataField="password" HeaderText="password" SortExpression="password" />
                <asp:BoundField DataField="montoasignado" HeaderText="montoasignado" SortExpression="montoasignado" />
                <asp:BoundField DataField="codcliente" HeaderText="codcliente" SortExpression="codcliente" />
                <asp:BoundField DataField="codinterno" HeaderText="codinterno" SortExpression="codinterno" />
                <asp:BoundField DataField="NroTarjeta" HeaderText="NroTarjeta" SortExpression="NroTarjeta" />
                <asp:BoundField DataField="estado" HeaderText="estado" SortExpression="estado" />
                <asp:BoundField DataField="email" HeaderText="email" SortExpression="email" />
            </Columns>
            <EditRowStyle BackColor="#2461BF" />
            <FooterStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
            <HeaderStyle BackColor="#507CD1" Font-Bold="True" ForeColor="White" />
            <PagerStyle BackColor="#2461BF" ForeColor="White" HorizontalAlign="Center" />
            <RowStyle BackColor="#EFF3FB" />
            <SelectedRowStyle BackColor="#D1DDF1" Font-Bold="True" ForeColor="#333333" />
            <SortedAscendingCellStyle BackColor="#F5F7FB" />
            <SortedAscendingHeaderStyle BackColor="#6D95E1" />
            <SortedDescendingCellStyle BackColor="#E9EBEF" />
            <SortedDescendingHeaderStyle BackColor="#4870BE" />
        </asp:GridView>
        <asp:SqlDataSource ID="SqlDataSource1" runat="server" ConnectionString="<%$ ConnectionStrings:BDTRansactionConnectionString %>" SelectCommand="SELECT * FROM [Usuario]" ProviderName="System.Data.SqlClient"></asp:SqlDataSource>
    </div>
    </form>
</body>
</html>
