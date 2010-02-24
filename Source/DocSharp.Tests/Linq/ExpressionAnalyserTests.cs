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

        [Test]
        public void Should_match_method_call_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Address1.Contains("H");
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 5 } }));
        }

        [Test]
        public void Shouldnt_match_method_call_expression()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Address1.Contains("123H");
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 5 } }));
        }

        [Test]
        public void Should_match_double_conditions_in_expresion()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Address1.Contains("H") && q.Data.Phone == 5;
            Assert.True(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 5 } }));
        }

        [Test]
        public void Shouldnt_match_double_conditions_in_expresion()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Address1.Contains("H") && q.Data.Phone < 5;
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Phone = 5 } }));
        }

        [Test]
        public void Should_match_triple_conditions_in_expresion()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Address1.Contains("H") && q.Data.Address2.Contains("R") && q.Data.Phone == 5;
            Assert.IsTrue(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Address2 = "Road",  Phone = 5 } }));
        }

        [Test]
        public void Shouldnt_match_triple_conditions_in_expresion()
        {
            Expression<Func<Document<Company>, bool>> expression = q => q.Data.Address1.Contains("1") && q.Data.Address2.Contains("R") && q.Data.Phone == 5;
            Assert.IsFalse(ExpressionAnalyser.Matches(expression, new Document<Company> { Data = new Company { Address1 = "Hello", Address2 = "Road", Phone = 5 } }));
        }

    }
}