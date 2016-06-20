using Lucene.Net.Analysis;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Ranta.LuceneDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = "IndexFolder";

            var list = new List<Meta>();

            DirectoryInfo root = new DirectoryInfo("Articles");
            foreach (var file in root.GetFiles())
            {
                var meta = new Meta();

                meta.Title = file.Name.Replace(".txt", string.Empty);
                meta.Url = file.FullName;
                meta.Content = File.ReadAllText(file.FullName);

                list.Add(meta);
            }

            //创建索引
            CreateIndex(path, list);

            //搜索关键词
            var result = Search(path, "能量");

            var result2 = Search(path, "book");

            Console.WriteLine("Done.");
            Console.ReadKey();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path">文件夹，索引存放位置</param>
        /// <param name="sources"></param>
        static void CreateIndex(string path, List<Meta> sources)
        {
            var root = new DirectoryInfo(path);
            var fsLockFactory = new NativeFSLockFactory();

            using (FSDirectory fsRoot = FSDirectory.Open(root, fsLockFactory))
            {
                //创建向索引库写操作对象
                //使用IndexWriter打开directory时会自动对索引库文件上锁
                //Analyzer analyzer = new SimpleAnalyzer();
                Analyzer analyzer = new PanGuAnalyzer();
                using (IndexWriter writer = new IndexWriter(fsRoot, analyzer, !IndexReader.IndexExists(fsRoot), IndexWriter.MaxFieldLength.UNLIMITED))
                {
                    foreach (var source in sources)
                    {
                        Document document = new Document();

                        document.Add(new Field("Title", source.Title, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));

                        document.Add(new Field("Url", source.Url, Field.Store.YES, Field.Index.NOT_ANALYZED));

                        document.Add(new Field("Content", source.Content, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));

                        writer.AddDocument(document);
                    }
                }
            }
        }

        static List<Meta> Search(string path, string keywords)
        {
            var list = new List<Meta>();

            var root = new DirectoryInfo(path);
            var fsLockFactory = new NativeFSLockFactory();

            FSDirectory fsRoot = FSDirectory.Open(root, fsLockFactory);
            IndexReader reader = IndexReader.Open(fsRoot, true);
            IndexSearcher searcher = new IndexSearcher(reader);

            PhraseQuery query = new PhraseQuery();
            var words = SplitKeywords(keywords);
            foreach (var word in words)
            {
                query.Add(new Term("Content", word));
            }
            query.Slop = 100;

            TopScoreDocCollector collector = TopScoreDocCollector.Create(200, true);

            searcher.Search(query, null, collector);

            ScoreDoc[] docs = collector.TopDocs(0, collector.TotalHits).ScoreDocs;

            foreach (var doc in docs)
            {
                var document = searcher.Doc(doc.Doc);

                Meta meta = new Meta();

                meta.Title = document.Get("Title");
                meta.Url = document.Get("Url");
                meta.Content = document.Get("Content");

                list.Add(meta);
            }

            return list;
        }

        static List<string> SplitKeywords(string keywords)
        {
            var list = new List<string>();

            //Analyzer analyzer = new SimpleAnalyzer();
            Analyzer analyzer = new PanGuAnalyzer();
            TokenStream tokenStream = analyzer.TokenStream(string.Empty, new StringReader(keywords));
            ITermAttribute term = tokenStream.AddAttribute<ITermAttribute>();

            while (tokenStream.IncrementToken())
            {
                if (tokenStream.HasAttribute<ITermAttribute>())
                {
                    list.Add(tokenStream.GetAttribute<ITermAttribute>().Term);
                }
            }

            return list;
        }
    }
}
