using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UniformDataOperator.AssembliesManagement;
using UniformDataOperator.Sql.Markup;
using UniformDataOperator.Sql.MySql.Markup;
using MySql.Data.MySqlClient;

namespace BaseTests.Members
{
    [TestClass]
    public class MembersHandlerTests
    {
        /// <summary>
        /// Adding an attribute during runtime.
        /// </summary>
        [TestMethod]
        public void RuntimeAddingAttributte()
        {
            try
            {
                var type = MembersHandler.AddAttribute(
                    "RuntimeAssembly",
                    typeof(Types.Table2Type),

                    new MembersHandler.RuntimeAttributeInfo(
                        typeof(CommentaryAttribute),
                        "TestCommentary"),

                    new MembersHandler.RuntimeAttributeInfo(
                        typeof(MySqlDBTypeOverrideAttribute),
                        MySqlDbType.VarChar));

                bool result = MembersHandler.HasAttribute<CommentaryAttribute>(type);
                result &= MembersHandler.HasAttribute<MySqlDBTypeOverrideAttribute>(type);
                Assert.IsTrue(result);
            }
            catch(Exception ex)
            {
                Assert.Fail(ex.Message);
            }
        }
    }
}
