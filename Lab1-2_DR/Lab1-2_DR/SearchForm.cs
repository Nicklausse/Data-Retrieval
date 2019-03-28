using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Npgsql;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;

namespace Lab1_2_DR
{
    public partial class SearchForm : Form
    {
        public static String indexLocation = System.IO.Directory.GetCurrentDirectory();
        public static FSDirectory dir = FSDirectory.Open(indexLocation);
        public static LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;
        public static StandardAnalyzer analyzer = new StandardAnalyzer(AppLuceneVersion);

        public static IndexWriterConfig indexConfig = new IndexWriterConfig(AppLuceneVersion, analyzer);
        public static IndexWriter writer = new IndexWriter(dir, indexConfig);

        public SearchForm()
        {
            InitializeComponent();

            bool boolfound = false;

            using (NpgsqlConnection conn = new NpgsqlConnection("Server=84.201.147.162; Port=5432; User Id=developer; Password=rtfP@ssw0rd; Database=Data-Retrieval"))
            {
                conn.Open();

                NpgsqlCommand cmd = new NpgsqlCommand("SELECT * FROM movies", conn);
                NpgsqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    boolfound = true;
                    Console.WriteLine("Connection established");
                }
                if (boolfound == false)
                {
                    Console.WriteLine("Data does not exist");
                }
                dr.Close();

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    try
                    {
                        var movieId = reader.GetInt32(0);
                        var movieName = reader.GetString(1);
                        var movieYear = 0;
                        int.TryParse(reader.GetInt16(2).ToString(), out movieYear);

                        var doc = new Document();
                        doc.Add(new Field("fullName", movieName, StringField.TYPE_STORED));
                       
                        foreach (var word in movieName.Split(' '))
                        {
                            if (!String.IsNullOrEmpty(word))
                                doc.Add(new Field("wordName", word, TextField.TYPE_STORED));
                        }
                        doc.Add(new StoredField("id", movieId));
                        doc.Add(new Int32Field("year", movieYear, Field.Store.YES));
                        writer.AddDocument(doc);
                        this.searchResult.Rows.Add(reader.GetInt32(0), reader.GetString(1), movieYear.ToString());
                    }
                    catch
                    {
                        //
                    }
                }
            }
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            var query = this.searchQuery.Text;
            int queryIsString = 0;
            int.TryParse(query, out queryIsString);

            var oneOfWordsSearchInName = "SELECT * FROM movies WHERE name ILIKE '% ' || @string || ' %' OR name ILIKE @string || ' %' OR name ILIKE '% ' || @string OR name = @string LIMIT 10";
            var partSearchInName = "SELECT * FROM movies WHERE name ILIKE '%' || @string || '%' LIMIT 10";
            var allWordsSearchInName = "SELECT * FROM movies WHERE name = @string LIMIT 10";
            var partSearchOrYearInName = queryIsString == 0 ? "SELECT * FROM movies WHERE name ILIKE '%' || @string || '%' LIMIT 10" : "SELECT * FROM movies WHERE year = " + query + " LIMIT 10";

            var searchAll = "SELECT* FROM movies";

            var resultItems = new List<(int id, string name, string year)>();

            Cursor.Current = Cursors.WaitCursor;

            bool boolfound = false;

            //поиск по одному из слов в названии фильма
            using (NpgsqlConnection conn = new NpgsqlConnection("Server=84.201.147.162; Port=5432; User Id=developer; Password=rtfP@ssw0rd; Database=Data-Retrieval"))
            {
                conn.Open();

                NpgsqlCommand cmd = new NpgsqlCommand(oneOfWordsSearchInName, conn); 

                if (query == " ")
                {
                    cmd = new NpgsqlCommand(searchAll, conn);
                }

                cmd.Parameters.Add("@string", NpgsqlTypes.NpgsqlDbType.Text);
                cmd.Parameters["@string"].Value = query;
                cmd.Parameters.AddWithValue(query);

                NpgsqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    boolfound = true;
                    Console.WriteLine("Connection established");
                }
                if (boolfound == false)
                {
                    Console.WriteLine("Data does not exist");
                }
                dr.Close();
                var reader = cmd.ExecuteReader();
                this.searchResult.Rows.Clear();

                while (reader.Read())
                {
                    var year = "";
                    try
                    {
                        year = reader.GetInt16(2).ToString();
                    }
                    catch
                    {
                        year = "";
                    }

                    if (!resultItems.Any(f => f.id == reader.GetInt32(0)))
                        resultItems.Add((reader.GetInt32(0), reader.GetString(1), year));
                }

            }

            //поиск по всем словам из названия фильма
            using (NpgsqlConnection conn = new NpgsqlConnection("Server=84.201.147.162; Port=5432; User Id=developer; Password=rtfP@ssw0rd; Database=Data-Retrieval"))
            {
                conn.Open();

                NpgsqlCommand cmd = new NpgsqlCommand(allWordsSearchInName, conn); 

                if (query == " ")
                {
                    cmd = new NpgsqlCommand(searchAll, conn);
                }

                cmd.Parameters.Add("@string", NpgsqlTypes.NpgsqlDbType.Text);
                cmd.Parameters["@string"].Value = query;
                cmd.Parameters.AddWithValue(query);

                NpgsqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    boolfound = true;
                    Console.WriteLine("Connection established");
                }
                if (boolfound == false)
                {
                    Console.WriteLine("Data does not exist");
                }
                dr.Close();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var year = "";
                    try
                    {
                        year = reader.GetInt16(2).ToString();
                    }
                    catch
                    {
                        year = "";
                    }

                    if (!resultItems.Any(f => f.id == reader.GetInt32(0)))
                        resultItems.Add((reader.GetInt32(0), reader.GetString(1), year));
                }
            }

            //поиск по частичным словам из названия фильма
            using (NpgsqlConnection conn = new NpgsqlConnection("Server=84.201.147.162; Port=5432; User Id=developer; Password=rtfP@ssw0rd; Database=Data-Retrieval"))
            {
                conn.Open();

                NpgsqlCommand cmd = new NpgsqlCommand(partSearchInName, conn); 

                if (query == " ")
                {
                    cmd = new NpgsqlCommand(searchAll, conn);
                }

                cmd.Parameters.Add("@string", NpgsqlTypes.NpgsqlDbType.Text);
                cmd.Parameters["@string"].Value = query;
                cmd.Parameters.AddWithValue(query);

                NpgsqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    boolfound = true;
                    Console.WriteLine("Connection established");
                }
                if (boolfound == false)
                {
                    Console.WriteLine("Data does not exist");
                }
                dr.Close();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var year = "";
                    try
                    {
                        year = reader.GetInt16(2).ToString();
                    }
                    catch
                    {
                        year = "";
                    }

                    if (!resultItems.Any(f => f.id == reader.GetInt32(0)))
                        resultItems.Add((reader.GetInt32(0), reader.GetString(1), year));
                }
            }

            //поиск по году и названию (части названия)
            using (NpgsqlConnection conn = new NpgsqlConnection("Server=84.201.147.162; Port=5432; User Id=developer; Password=rtfP@ssw0rd; Database=Data-Retrieval"))
            {
                conn.Open();

                NpgsqlCommand cmd = new NpgsqlCommand(partSearchOrYearInName, conn); 

                if (query == " ")
                {
                    cmd = new NpgsqlCommand(searchAll, conn);
                }

                cmd.Parameters.Add("@string", NpgsqlTypes.NpgsqlDbType.Text);
                cmd.Parameters["@string"].Value = query;
                cmd.Parameters.AddWithValue(query);

                NpgsqlDataReader dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    boolfound = true;
                    Console.WriteLine("connection established");
                }
                if (boolfound == false)
                {
                    Console.WriteLine("Data does not exist");
                }
                dr.Close();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var year = "";
                    try
                    {
                        year = reader.GetInt16(2).ToString();
                    }
                    catch
                    {
                        year = "";
                    }

                    if (!resultItems.Any(f => f.id == reader.GetInt32(0)))
                        resultItems.Add((reader.GetInt32(0), reader.GetString(1), year));
                }
                Cursor.Current = Cursors.Default;

                foreach (var resultItem in resultItems)
                {
                    searchResult.Rows.Add(resultItem.id, resultItem.name, resultItem.year);
                }
            }

        }

        private void searchQuery_TextChanged(object sender, EventArgs e)
        {
            searchButton.Enabled = searchQuery.Text == "" ? false : true;
            luceneSearchButton.Enabled = searchQuery.Text == "" ? false : true;
        }

        private void luceneSearchButton_Click(object sender, EventArgs e)
        {
            int counter = 0;
            this.searchResult.Rows.Clear();
            var query = this.searchQuery.Text.ToLower();
            var array = query.Split(' ').ToList();
            List<string> res_list = new List<string>();
            var searcher = new IndexSearcher(writer.GetReader(applyAllDeletes: true));

            var totalResults = new List<Document>();

            //поиск по одному слову из названия
            var phrase = new MultiPhraseQuery();
            foreach (var word in array)
            {
                phrase = new MultiPhraseQuery();
                if (!String.IsNullOrEmpty(word))
                {
                    phrase.Add(new Term("wordName", word));
                    var res = searcher.Search(phrase, 10).ScoreDocs;
                    foreach (var hit in res)
                    {
                        var foundDoc = searcher.Doc(hit.Doc);
                        if (!totalResults.Any(f =>
                            f.GetField("id").GetInt32Value() == foundDoc.GetField("id").GetInt32Value()))
                            totalResults.Add(foundDoc);
                    }
                }
            }

            //поиск по всем словам названия
            phrase = new MultiPhraseQuery();
            phrase.Add(new Term("fullName", query));
            var hits = searcher.Search(phrase, 10).ScoreDocs;
            foreach (var hit in hits)
            {
                var foundDoc = searcher.Doc(hit.Doc);
                if (!totalResults.Any(f => f.GetField("id").GetInt32Value() == foundDoc.GetField("id").GetInt32Value()))
                    totalResults.Add(foundDoc);
            }

            //поиск по частичным словам названия
            foreach (var word in array)
            {
                if (!String.IsNullOrEmpty(word))
                {
                    var wild = new WildcardQuery(new Term("wordName", "*" + word + "*"));
                    var res = searcher.Search(wild, 10).ScoreDocs;
                    foreach (var hit in res)
                    {
                        var foundDoc = searcher.Doc(hit.Doc);
                        if (!totalResults.Any(f =>
                            f.GetField("id").GetInt32Value() == foundDoc.GetField("id").GetInt32Value()))
                            totalResults.Add(foundDoc);
                    }
                }
            }

            //поиск по году и названию (части названия)
            string year_to_find = "";
            int number = 0;
            foreach (var word in array)
            {
                bool result = Int32.TryParse(word, out number);
                if (result && number > 1800 && number <= 9999)
                {
                    year_to_find = word;
                    array.RemoveAt(array.IndexOf(word));
                    break;
                }
            }
            Console.WriteLine(number != 0);

            if (number != 0)
            {
                phrase = new MultiPhraseQuery();
                foreach (var word in array)
                {
                    if (!String.IsNullOrEmpty(word))
                    {
                        BooleanQuery booleanQuery = new BooleanQuery();

                        var wild = new WildcardQuery(new Term("wordName", "*" + word + "*"));
                        var num = NumericRangeQuery.NewInt32Range("year", 1, number, number, true, true);

                        booleanQuery.Add(wild, Occur.SHOULD);
                        booleanQuery.Add(num, Occur.SHOULD);
                        var res = searcher.Search(booleanQuery, 10).ScoreDocs;
                        foreach (var hit in res)
                        {
                            var foundDoc = searcher.Doc(hit.Doc);
                            if (!totalResults.Any(f =>
                                f.GetField("id").GetInt32Value() == foundDoc.GetField("id").GetInt32Value()))
                                totalResults.Add(foundDoc);
                        }
                    }
                }
            }
            foreach (var doc in totalResults)
            {
                searchResult.Rows.Add(doc.GetField("id").GetInt32Value().ToString(),
                    doc.GetValues("fullName")[0],
                    doc.GetField("year").GetInt32Value().ToString());
            }
        }
    }
}
