using System;
using System.Linq.Expressions;
using DocSharp.Linq;
using DocSharp.Tests.TestFixtures;
using NUnit.Framework;

namespace DocSharp.Tests.Linq
{
    [TestFixture]
    public class ExpressionAnalyserTests 
    {
        [Test]
        public void Should_match_basic_equals()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Address1 == "Hello";
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> {Data = new Company {Address1 = "Hello"}}));
        }

        [Test]
        public void Should_match_less_than()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Phone < 1;
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 0} }));
        }

        [Test]
        public void Shouldnt_match_less_than()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Phone < 1;
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 1 } }));
        }

        [Test]
        public void Should_match_less_than_equal_to()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Phone <= 1;
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 1 } }));
        }

        [Test]
        public void Shouldnt_match_less_than_equal_to()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Phone <= 1;
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 2 } }));
        }

        [Test]
        public void Should_match_greater_than_equal_to()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Phone >= 1;
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 1 } }));
        }

        [Test]
        public void Shouldnt_match_greater_than_equal_to()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Phone >= 1;
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 0 } }));
        }

        [Test]
        public void Should_match_greater_than()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Phone > 1;
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 2 } }));
        }

        [Test]
        public void Shouldnt_match_greater_than()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Phone > 1;
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 0 } }));
        }

        [Test]
        public void Should_match_basic_equals_reverse_the_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => "Hello" == q.Data.Address1 ;
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> {Data = new Company {Address1 = "Hello"}}));
        }

        [Test]
        public void Should_match_less_than_reverse_the_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => 1 < q.Data.Phone;
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 5} }));
        }

        [Test]
        public void Shouldnt_match_less_than_reverse_the_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => 1 < q.Data.Phone;
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 1 } }));
        }

        [Test]
        public void Should_match_less_than_equal_to_reverse_the_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => 1 <= q.Data.Phone;
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 1 } }));
        }

        [Test]
        public void Shouldnt_match_less_than_equal_to_reverse_the_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => 1 <= q.Data.Phone;
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 0 } }));
        }

        [Test]
        public void Should_match_greater_than_equal_to_reverse_the_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => 1 >= q.Data.Phone;
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 1 } }));
        }

        [Test]
        public void Shouldnt_match_greater_than_equal_to_reverse_the_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => 1 >= q.Data.Phone;
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 5 } }));
        }

        [Test]
        public void Should_match_greater_than_reverse_the_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => 1 > q.Data.Phone;
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 0 } }));
        }

        [Test]
        public void Shouldnt_match_greater_than_reverse_the_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => 1 > q.Data.Phone;
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 5 } }));
        }
    }
}