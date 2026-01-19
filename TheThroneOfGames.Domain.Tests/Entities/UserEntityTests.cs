using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TheThroneOfGames.Domain.Tests.Entities
{
    [TestClass]
    public class UserEntityTests
    {
        [TestMethod]
        public void UserEntity_TypeExists()
        {
            // Arrange & Act
            var type = Type.GetType("TheThroneOfGames.Domain.Entities.UserEntity, TheThroneOfGames.Domain");

            // Assert
            Assert.IsNotNull(type, "Tipo UserEntity não encontrado no assembly Domain. Verifique namespace e nome de classe.");
        }

        [TestMethod]
        public void CanCreateUserEntity_Instance()
        {
            // Arrange
            var type = Type.GetType("TheThroneOfGames.Domain.Entities.UserEntity, TheThroneOfGames.Domain");
            
            // Act
            var instance = type is null ? null : Activator.CreateInstance(type);

            // Assert
            Assert.IsNotNull(instance, "Não foi possível instanciar UserEntity (verifique construtor público sem parâmetros).");
        }
    }
}
