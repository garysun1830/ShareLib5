using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.IO;

namespace ShareLib5
{
    public delegate void FileToRow(DataRow Row, FileInfo Info, object Data);
    public interface IFolderAction
    {
        bool execute(string Dir);
    }

    public class MyIO
    {

        public static DataTable PrepareImageTable(string TableName, string KeyName, string ColName)
        {
            DataTable table = new DataTable(TableName);
            DataColumn id = new DataColumn();
            id.DataType = System.Type.GetType("System.Int32");
            id.ColumnName = KeyName;
            id.AutoIncrement = true;
            table.Columns.Add(id);
            DataColumn img = new DataColumn();
            img.DataType = System.Type.GetType("System.String");
            img.ColumnName = ColName;
            img.DefaultValue = "";
            table.Columns.Add(img);
            DataColumn[] keys = new DataColumn[1];
            keys[0] = id;
            table.PrimaryKey = keys;
            return table;
        }

        public static void BrowseFileToTable(string Folder, string FileAttr, DataTable Table, object Data, FileToRow FileToRowDelegate)
        {
            DirectoryInfo di = new DirectoryInfo(Folder);
            FileInfo[] files = di.GetFiles(FileAttr);
            string imgcol = "";
            foreach (DataColumn col in Table.Columns)
                if (col.DataType == System.Type.GetType("System.String"))
                {
                    imgcol = col.ColumnName;
                    break;
                }
            if (imgcol == "")
                return;
            foreach (FileInfo f in files)
            {
                DataRow row = Table.NewRow();
                if (FileToRowDelegate == null)
                    row[imgcol] = f.Name;
                else
                    FileToRowDelegate(row, f, Data);
                Table.Rows.Add(row);
            }
        }

        public static void BrowseFileToTable(string Folder, string FileAttr, DataTable Table)
        {
            BrowseFileToTable(Folder, FileAttr, Table, null, null);
        }

        public static void ActOnFolder(string Dir, IFolderAction act)
        {
            act.execute(Dir);
        }

    }

    public class FolderImageSource
    {
        private string _imageCol;
        private string _titleCol;
        private string _sortText;
        private string _pageCol;
        private string _folder;
        private string _fileAttr;
        private int _pageCount;
        private FileInfo[] _files;
        public event FileToRow AddFileToRow;
        public event EventHandler SortFiles;

        public FolderImageSource(string ImageCol, string TitleCol, string SortText, string PageCol, string Folder, string FileAttr, int PageCount)
        {
            if (PageCount < 1)
                throw new Exception("Page count is not valid.");
            _imageCol = ImageCol;
            _titleCol = TitleCol;
            _sortText = SortText;
            _pageCol = PageCol;
            _folder = Folder;
            _fileAttr = FileAttr;
            _pageCount = PageCount;
        }

        private void GetFiles()
        {
            DirectoryInfo di = new DirectoryInfo(MyFunc.CurrentServer().MapPath(_folder));
            _files = di.GetFiles(_fileAttr);
            if (SortFiles != null)
                SortFiles(_files, null);
        }

        public void LoadFiles()
        {
            GetFiles();
            CreatePagingTable();
        }

        public DataTable CreatePagingTable()
        {
            string[] img = { _pageCol };
            string[] dtype = { "System.String" };
            DataTable table = DatabaseHelper.CreateTable("", "Index", true, img, dtype);
            DataRow row = table.NewRow();
            row[_pageCol] = "<<";
            table.Rows.Add(row);
            for (int i = 0; i < PageTotal(); i++)
            {
                row = table.NewRow();
                int j = i + 1;
                row[_pageCol] = j.ToString();
                table.Rows.Add(row);
            }
            row = table.NewRow();
            row[_pageCol] = ">>";
            table.Rows.Add(row);
            return table;
        }

        public int PageTotal()
        {
            int n = _files.Length / _pageCount;
            if (_files.Length % _pageCount > 0)
                n++;
            return n;
        }

        public DataTable GotoPage(int Number)
        {
            string[] img = { _imageCol, _titleCol };
            string[] dtype = { "System.String", "System.String" };
            DataTable table = DatabaseHelper.CreateTable("", "Index", true, img, dtype);
            int j = (Number - 1) * _pageCount;
            for (int i = j; i < j + _pageCount && i < _files.Length; i++)
            {
                FileInfo f = _files[i];
                DataRow row = table.NewRow();
                if (AddFileToRow == null)
                    row[_imageCol] = _folder + "/" + f.Name;
                else
                    AddFileToRow(row, f, _folder);
                table.Rows.Add(row);
                table.DefaultView.Sort = _sortText;
            }
            return table;
        }

    }

}