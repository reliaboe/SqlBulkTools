﻿//using System.Collections.Generic;
//using System.Configuration;
//using System.Data;
//using System.Data.Entity;
//using System.Data.SqlClient;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Transactions;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Ploeh.AutoFixture;
//using SqlBulkTools.Enumeration;
//using SqlBulkTools.IntegrationTests.Helper;
//using SqlBulkTools.TestCommon.Model;

//namespace SqlBulkTools.IntegrationTests
//{
//    [TestClass]
//    public class BulkOperationsAsyncIT
//    {
//        private const int RepeatTimes = 1;

//        private BookRandomizer _randomizer;
//        private DataAccess _dataAccess;
//        private List<Book> _bookCollection;

//        [TestInitialize]
//        public void Setup()
//        {
//            _dataAccess = new DataAccess();
//            _randomizer = new BookRandomizer();
//        }

//        [TestMethod]
//        public async Task SqlBulkTools_BulkDeleteWithSelectedColumns_TestIdentityOutput()
//        {

//            _db.Books.RemoveRange();
//            await _db.SaveChangesAsync();

//            using (
//                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["SqlBulkToolsTest"].ConnectionString)
//                )
//            using (var command = new SqlCommand(
//                "DBCC CHECKIDENT ('[dbo].[Books]', RESEED, 10);", conn)
//            {
//                CommandType = CommandType.Text
//            })
//            {
//                conn.Open();
//                await command.ExecuteNonQueryAsync();
//            }

//            List<Book> books = _randomizer.GetRandomCollection(30);
//            await BulkInsertAsync(books);

//            BulkOperations bulk = new BulkOperations();

//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<Book>()
//                        .ForCollection(books)
//                        .WithTable("Books")
//                        .WithBulkCopySettings(new BulkCopySettings()
//                        {
//                            BatchSize = 5000
//                        })
//                        .AddColumn(x => x.ISBN)
//                        .BulkDelete()
//                        .MatchTargetOn(x => x.ISBN)
//                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
//                        .CommitAsync(conn);
//                }

//                trans.Complete();
//            }

//            var test = books.First();
//            var expected = 11;

//            Assert.AreEqual(expected, test.Id);
//        }

//        public async Task SqlBulkTools_BulkInsertAsync()
//        {
//            const int rows = 1000;

//            await BulkDeleteAsync(_db.Books.ToList());
//            _bookCollection = new List<Book>();
//            _bookCollection.AddRange(_randomizer.GetRandomCollection(rows));
//            List<long> results = new List<long>();

//            Trace.WriteLine("Testing BulkInsertAsync with " + rows + " rows");

//            for (int i = 0; i < RepeatTimes; i++)
//            {
//                long time = await BulkInsertAsync(_bookCollection);
//                results.Add(time);
//            }
//            double avg = results.Average(l => l);
//            Trace.WriteLine("Average result (" + RepeatTimes + " iterations): " + avg.ToString("#.##") + " ms\n\n");

//            Assert.AreEqual(rows * RepeatTimes, _db.Books.Count());
//        }

//        public async Task SqlBulkTools_BulkInsertOrUpdateAsync()
//        {
//            const int rows = 500, newRows = 500;

//            await BulkDeleteAsync(_db.Books.ToList());
//            var fixture = new Fixture();
//            _bookCollection = _randomizer.GetRandomCollection(rows);

//            List<long> results = new List<long>();

//            Trace.WriteLine("Testing BulkInsertOrUpdateAsync with " + (rows + newRows) + " rows");

//            for (int i = 0; i < RepeatTimes; i++)
//            {
//                await BulkInsertAsync(_bookCollection);

//                // Update some rows
//                for (int j = 0; j < 200; j++)
//                {
//                    var newBook = fixture.Build<Book>().Without(s => s.ISBN).Create();
//                    var prevIsbn = _bookCollection[j].ISBN;
//                    _bookCollection[j] = newBook;
//                    _bookCollection[j].ISBN = prevIsbn;
//                }

//                // Add new rows
//                _bookCollection.AddRange(_randomizer.GetRandomCollection(newRows));


//                long time = await BulkInsertOrUpdateAsync(_bookCollection);
//                results.Add(time);

//                Assert.AreEqual(rows + newRows, _db.Books.Count());

//            }

//            double avg = results.Average(l => l);
//            Trace.WriteLine("Average result (" + RepeatTimes + " iterations): " + avg.ToString("#.##") + " ms\n\n");


//        }

//        public async Task SqlBulkTools_BulkUpdateAsync()
//        {
//            const int rows = 1000;

//            var fixture = new Fixture();
//            fixture.Customizations.Add(new PriceBuilder());
//            fixture.Customizations.Add(new IsbnBuilder());
//            fixture.Customizations.Add(new TitleBuilder());

//            await BulkDeleteAsync(_db.Books.ToList());

//            List<long> results = new List<long>();

//            Trace.WriteLine("Testing BulkUpdateAsync with " + rows + " rows");

//            for (int i = 0; i < RepeatTimes; i++)
//            {

//                _bookCollection = _randomizer.GetRandomCollection(rows);
//                await BulkInsertAsync(_bookCollection);

//                // Update half the rows
//                for (int j = 0; j < rows / 2; j++)
//                {
//                    var newBook = fixture.Build<Book>().Without(s => s.Id).Without(s => s.ISBN).Create();
//                    var prevIsbn = _bookCollection[j].ISBN;
//                    _bookCollection[j] = newBook;
//                    _bookCollection[j].ISBN = prevIsbn;

//                }

//                long time = await BulkUpdateAsync(_bookCollection);
//                results.Add(time);

//                var testUpdate = await _db.Books.FirstOrDefaultAsync();
//                Assert.AreEqual(_bookCollection[0].Price, testUpdate.Price);
//                Assert.AreEqual(_bookCollection[0].Title, testUpdate.Title);
//                Assert.AreEqual(_db.Books.Count(), _bookCollection.Count);

//                await BulkDeleteAsync(_bookCollection);
//            }
//            double avg = results.Average(l => l);
//            Trace.WriteLine("Average result (" + RepeatTimes + " iterations): " + avg.ToString("#.##") + " ms\n\n");

//        }

//        public async Task SqlBulkTools_BulkDeleteAsync()
//        {
//            const int rows = 500;

//            _bookCollection = _randomizer.GetRandomCollection(rows);
//            await BulkDeleteAsync(_db.Books.ToList());

//            List<long> results = new List<long>();

//            Trace.WriteLine("Testing BulkDeleteAsync with " + rows + " rows");

//            for (int i = 0; i < RepeatTimes; i++)
//            {
//                await BulkInsertAsync(_bookCollection);
//                long time = await BulkDeleteAsync(_bookCollection);
//                results.Add(time);
//                Assert.AreEqual(0, _db.Books.Count());
//            }
//            double avg = results.Average(l => l);
//            Trace.WriteLine("Average result (" + RepeatTimes + " iterations): " + avg.ToString("#.##") + " ms\n\n");

//        }

//        [TestMethod]
//        public async Task SqlBulkTools_BulkInsertOrUpdateAsync_TestIdentityOutput()
//        {
//            _db.Books.RemoveRange(_db.Books.ToList());
//            await _db.SaveChangesAsync();
//            BulkOperations bulk = new BulkOperations();

//            List<Book> books = _randomizer.GetRandomCollection(30);

//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<Book>()
//                        .ForCollection(books)
//                        .WithTable("Books")
//                        .AddAllColumns()
//                        .BulkInsertOrUpdate()
//                        .MatchTargetOn(x => x.ISBN)
//                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
//                        .CommitAsync(conn);
//                }

//                trans.Complete();
//            }

//            var test = _db.Books.ToList().ElementAt(10); // Random book within the 30 elements
//            var expected = books.Single(x => x.ISBN == test.ISBN);

//            Assert.AreEqual(expected.Id, test.Id);

//        }

//        [TestMethod]
//        public async Task SqlBulkTools_BulkInsertOrUpdateAsyncWithSelectedColumns_TestIdentityOutput()
//        {
//            _db.Books.RemoveRange(_db.Books.ToList());
//            await _db.SaveChangesAsync();
//            BulkOperations bulk = new BulkOperations();

//            List<Book> books = _randomizer.GetRandomCollection(30);

//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<Book>()
//                        .ForCollection(books)
//                        .WithTable("Books")
//                        .AddColumn(x => x.ISBN)
//                        .AddColumn(x => x.Description)
//                        .AddColumn(x => x.Title)
//                        .AddColumn(x => x.Price)
//                        .BulkInsertOrUpdate()
//                        .MatchTargetOn(x => x.ISBN)
//                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
//                        .CommitAsync(conn);
//                }

//                trans.Complete();
//            }

//            var test = _db.Books.ToList().ElementAt(10); // Random book within the 30 elements
//            var expected = books.Single(x => x.ISBN == test.ISBN);

//            Assert.AreEqual(expected.Id, test.Id);

//        }

//        [TestMethod]
//        public async Task SqlBulkTools_BulkInsertAsync_TestIdentityOutput()
//        {
//            _db.Books.RemoveRange(_db.Books.ToList());
//            await _db.SaveChangesAsync();

//            BulkOperations bulk = new BulkOperations();

//            List<Book> books = _randomizer.GetRandomCollection(30);

//            _db.Books.AddRange(_randomizer.GetRandomCollection(60)); // Add some random items before test. 
//            await _db.SaveChangesAsync();

//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<Book>()
//                        .ForCollection(books)
//                        .WithTable("Books")
//                        .AddAllColumns()
//                        .BulkInsert()
//                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
//                        .CommitAsync(conn);
//                }

//                trans.Complete();
//            }

//            var test = _db.Books.ToList().ElementAt(80); // Random between random items before test and total items after test. 
//            var expected = books.Single(x => x.ISBN == test.ISBN);

//            Assert.AreEqual(expected.Id, test.Id);

//        }

//        [TestMethod]
//        public async Task SqlBulkTools_BulkInsertAsyncWithSelectedColumns_TestIdentityOutput()
//        {

//            _db.Books.RemoveRange(_db.Books.ToList());
//            await _db.SaveChangesAsync();

//            List<Book> books = _randomizer.GetRandomCollection(30);

//            BulkOperations bulk = new BulkOperations();

//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<Book>()
//                        .ForCollection(books)
//                        .WithTable("Books")
//                        .WithBulkCopySettings(new BulkCopySettings()
//                        {
//                            BatchSize = 5000
//                        })                        
//                        .AddColumn(x => x.Title)
//                        .AddColumn(x => x.Price)
//                        .AddColumn(x => x.Description)
//                        .AddColumn(x => x.ISBN)
//                        .AddColumn(x => x.PublishDate)                      
//                        .BulkInsert()
//                        .TmpDisableAllNonClusteredIndexes()
//                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
//                        .CommitAsync(conn);
//                }

//                trans.Complete();
//            }

//            var test = _db.Books.ToList().ElementAt(15); // Random book within the 30 elements
//            var expected = books.Single(x => x.ISBN == test.ISBN);

//            Assert.AreEqual(expected.Id, test.Id);
//        }

//        [TestMethod]
//        public async Task SqlBulkTools_BulkUpdateAsyncWithSelectedColumns_TestIdentityOutput()
//        {
//            _db.Books.RemoveRange(_db.Books.ToList());
//            _db.SaveChanges();
//            BulkOperations bulk = new BulkOperations();

//            List<Book> books = _randomizer.GetRandomCollection(30);
//            await BulkInsertAsync(books);

//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<Book>()
//                        .ForCollection(books)
//                        .WithTable("Books")
//                        .AddColumn(x => x.ISBN)
//                        .AddColumn(x => x.Description)
//                        .AddColumn(x => x.Title)
//                        .AddColumn(x => x.Price)
//                        .BulkUpdate()
//                        .MatchTargetOn(x => x.ISBN)
//                        .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
//                        .CommitAsync(conn);
//                }

//                trans.Complete();
//            }

//            var test = _db.Books.ToList().ElementAt(10); // Random book within the 30 elements
//            var expected = books.Single(x => x.ISBN == test.ISBN);

//            Assert.AreEqual(expected.Id, test.Id);
//        }

//        [TestMethod]
//        [ExpectedException(typeof(SqlBulkToolsException), "No setter method available on property 'Id'. Could not write output back to property.")]
//        public async Task SqlBulkTools_BulkInsertAsyncWithoutSetter_ThrowsMeaningfulException()
//        {
//            _db.Books.RemoveRange(_db.Books.ToList());
//            _db.SaveChanges();
//            BulkOperations bulk = new BulkOperations();

//            _bookCollection = _randomizer.GetRandomCollection(30);

//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup()
//                    .ForCollection(
//                        _bookCollection.Select(
//                            x => new { x.Description, x.ISBN, x.Id, x.Price }))
//                    .WithTable("Books")
//                    .AddColumn(x => x.Id)
//                    .AddColumn(x => x.Description)
//                    .AddColumn(x => x.ISBN)
//                    .AddColumn(x => x.Price)
//                    .BulkInsert()
//                    .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
//                    .CommitAsync(conn);
//                }

//                trans.Complete();
//            }     
//        }

//        [TestMethod]
//        [ExpectedException(typeof(SqlBulkToolsException), "No setter method available on property 'Id'. Could not write output back to property.")]
//        public async Task SqlBulkTools_BulkInsertOrUpdateAsyncWithPrivateIdentityField_ThrowsMeaningfulException()
//        {
//            _db.Books.RemoveRange(_db.Books.ToList());
//            _db.SaveChanges();
//            BulkOperations bulk = new BulkOperations();

//            List<Book> books = _randomizer.GetRandomCollection(30);
//            List<BookWithPrivateIdentity> booksWithPrivateIdentity = new List<BookWithPrivateIdentity>();

//            books.ForEach(x => booksWithPrivateIdentity.Add(new BookWithPrivateIdentity()
//            {
//                ISBN = x.ISBN,
//                Description = x.Description,
//                Price = x.Price

//            }));

//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<BookWithPrivateIdentity>()
//                                .ForCollection(booksWithPrivateIdentity)
//                                .WithTable("Books")
//                                .AddColumn(x => x.Id)
//                                .AddColumn(x => x.Description)
//                                .AddColumn(x => x.ISBN)
//                                .AddColumn(x => x.Price)
//                                .BulkInsertOrUpdate()
//                                .MatchTargetOn(x => x.ISBN)
//                                .SetIdentityColumn(x => x.Id, ColumnDirectionType.InputOutput)
//                                .CommitAsync(conn);
//                }

//                trans.Complete();
//            }     

//        }

//        private async Task<long> BulkInsertAsync(IEnumerable<Book> col)
//        {
//            BulkOperations bulk = new BulkOperations();

//            var watch = System.Diagnostics.Stopwatch.StartNew();
//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<Book>()
//                        .ForCollection(col)
//                        .WithTable("Books")
//                        .WithBulkCopySettings(new BulkCopySettings()
//                        {
//                            SqlBulkCopyOptions = SqlBulkCopyOptions.TableLock,
//                            BatchSize = 3000
//                        })                    
//                        .AddColumn(x => x.Title)
//                        .AddColumn(x => x.Price)
//                        .AddColumn(x => x.Description)
//                        .AddColumn(x => x.ISBN)
//                        .AddColumn(x => x.PublishDate)
//                        .BulkInsert()
//                        .CommitAsync(conn);
//                }

//                trans.Complete();
//            }          
//            watch.Stop();
//            var elapsedMs = watch.ElapsedMilliseconds;
//            return elapsedMs;
//        }

//        private async Task<long> BulkInsertOrUpdateAsync(IEnumerable<Book> col)
//        {
//            BulkOperations bulk = new BulkOperations();

//            var watch = System.Diagnostics.Stopwatch.StartNew();
//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<Book>()
//                        .ForCollection(col)
//                        .WithTable("Books")
//                        .AddColumn(x => x.Title)
//                        .AddColumn(x => x.Price)
//                        .AddColumn(x => x.Description)
//                        .AddColumn(x => x.ISBN)
//                        .AddColumn(x => x.PublishDate)
//                        .BulkInsertOrUpdate()
//                        .MatchTargetOn(x => x.ISBN)
//                        .CommitAsync(conn);
//                }

//                trans.Complete();
//            }

//            watch.Stop();
//            var elapsedMs = watch.ElapsedMilliseconds;
//            return elapsedMs;
//        }

//        private async Task<long> BulkUpdateAsync(IEnumerable<Book> col)
//        {
//            BulkOperations bulk = new BulkOperations();

//            var watch = System.Diagnostics.Stopwatch.StartNew();
//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<Book>()
//                        .ForCollection(col)
//                        .WithTable("Books")
//                        .AddColumn(x => x.Title)
//                        .AddColumn(x => x.Price)
//                        .AddColumn(x => x.Description)
//                        .AddColumn(x => x.PublishDate)
//                        .BulkUpdate()
//                        .MatchTargetOn(x => x.ISBN)
//                        .CommitAsync(conn);
//                }

//                trans.Complete();
//            }
//            watch.Stop();
//            var elapsedMs = watch.ElapsedMilliseconds;
//            return elapsedMs;
//        }

//        private async Task<long> BulkDeleteAsync(IEnumerable<Book> col)
//        {
//            BulkOperations bulk = new BulkOperations();

//            var watch = System.Diagnostics.Stopwatch.StartNew();

//            using (TransactionScope trans = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
//            {
//                using (SqlConnection conn = new SqlConnection(ConfigurationManager
//                    .ConnectionStrings["SqlBulkToolsTest"].ConnectionString))
//                {
//                    await bulk.Setup<Book>()
//                        .ForCollection(col)
//                        .WithTable("Books")
//                        .AddColumn(x => x.ISBN)
//                        .BulkDelete()
//                        .MatchTargetOn(x => x.ISBN)
//                        .CommitAsync(conn);
//                }

//                trans.Complete();
//            }

//            watch.Stop();
//            var elapsedMs = watch.ElapsedMilliseconds;
//            return elapsedMs;
//        }
//    }
//}
