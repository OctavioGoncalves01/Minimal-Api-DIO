using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using minimal_api.Dominio.Entidades;

namespace Test.Domain.Entidades
{
    [TestClass]
    public class VeiculoTest

    {
        [TestMethod]
        public void TestarGetSetPropriedades(){
            //Arrange
            var adm = new Veiculo();

            //act
            adm.Id = 3;
            adm.Nome = "Fiesta";
            adm.Marca = "Ford";
            adm.Ano = 1990;

            //Assert
            Assert.AreEqual(3, adm.Id);
            Assert.AreEqual("Fiesta", adm.Nome);
            Assert.AreEqual("Ford", adm.Marca);
            Assert.AreEqual(1990, adm.Ano);

        }
        
    }
}