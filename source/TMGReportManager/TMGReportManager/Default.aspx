<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="TMGReportManager.Default" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
<link rel="stylesheet" type="text/css" href="style.css"/>
    <title>TMG Report Manager 1.0</title>
</head>
<body>
    <form id="form1" runat="server" visible="True">
        <div class="mainForm">
            <!--             
             -->

            

            <asp:Button ID="hideForm" CssClass="absolute mainFormHidden" runat="server" OnClick="hideForm_Click" /> 
            <!--             -->
            <div class ="header">
            <asp:HyperLink ID="LogoText" NavigateUrl="~/Default.aspx" CssClass="absolute programName fontSegoe" runat="server">TMG Report Manager</asp:HyperLink>
            <asp:Label ID="ResultLabel" CssClass="absolute userName fontSegoe" runat="server">ALTVAGON\usov.ia</asp:Label> 
            </div>
            <!--             -->
            <div ID="Body" class ="Body" runat="server">
            <asp:Label ID="LastUpdateTime" CssClass="absolute lastUpdate fontSegoe grayFont" runat="server">Последнее обновление базы: 20.04.2019</asp:Label>
            <asp:Image ID="filterIcon" CssClass="absolute filterIcon" runat="server" ImageUrl="~/image/filter-256.png" />
            <asp:Button ID="filterListEnable" CssClass="absolute buttonFilterList" runat="server" Text="Исключения при добавлении в базу" OnClick="filterList_Click"/>

                <div class=" rezultView">
                    <div class="holder">
                    <asp:PlaceHolder ID="linkArea" runat="server"></asp:PlaceHolder>
                    </div>

                </div>
                <div class="absolute filterView" runat="server">
                    <asp:Label ID="filterViewLabelName" CssClass="absolute grayFont fontSegoe filterViewName" runat="server">Фильтр поиска:</asp:Label>
                    <asp:Label CssClass="absolute grayFont fontSegoe startFilter" runat="server">Искать с:</asp:Label>
                    <input id="Date0" class="absolute fontSegoe calendarType fromFilter" type="date" runat="server" />
                    <asp:Label CssClass="absolute grayFont fontSegoe endFilter" runat="server">по:</asp:Label>
                    <input id="Date1" class="absolute fontSegoe calendarType hierFilter" type="date"  runat="server" />
                    <asp:Label CssClass="absolute grayFont fontSegoe userLabel" runat="server">Выбрать пользователя:</asp:Label>
                    <asp:DropDownList ID="userDropDown" CssClass="absolute fontSegoe userDropDown" EnableViewState="true" runat="server"></asp:DropDownList>
                    <asp:Button ID="filterButton" CssClass="absolute buttonViewFilterList" runat="server" Text="Применить фильтр" OnClick="filterButton_Click"/>


                    </div>
            </div>
            <!--             -->
            <div class =" footer">
                <asp:Label CssClass=" copyright fontSegoe" runat="server">Copyright &#169; 2019 Igor Usov </asp:Label>
                
            </div>
         </div>

        <div class="absolute filterForm" id="filterList" runat="server">
            <asp:Label CssClass="absolute filterUserLabel fontSegoe grayFont" AssociatedControlID="filterUser" runat="server">Список не учитываемых пользователей</asp:Label>
            <asp:TextBox ID="filterUser" TextMode="MultiLine" CssClass="absolute filterUser textBox" runat="server"></asp:TextBox>
            <asp:Label CssClass="absolute filterTargetLabel fontSegoe grayFont" AssociatedControlID="filterTarget" runat="server">Список не учитываемых хостов</asp:Label>
            <asp:TextBox ID="filterTarget" TextMode="MultiLine" CssClass="absolute filterTarget textBox" runat="server"></asp:TextBox>
            <asp:Label ID="filterLabel" CssClass="absolute filterLabel fontSegoe grayFont" AssociatedControlID="filter" runat="server">Список фильтров</asp:Label>
            <asp:TextBox ID="filter" TextMode="MultiLine" CssClass="absolute filter textBox" runat="server"></asp:TextBox>
            <asp:Button ID="saveFilterButton" CssClass="absolute fontSegoe saveButton" runat="server" Text="Сохранить изменения" OnClick="saveFilterButton_Click" OnClientClick="return confirm('Сохранить изменения в настройках фильтра?')" />
        </div>
    </form>
</body>
</html>
