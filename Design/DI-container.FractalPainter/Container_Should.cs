using System;
using System.Linq;
using System.Windows.Forms;
using Castle.DynamicProxy.Internal;
using FractalPainting.App;
using FractalPainting.App.Fractals;
using FractalPainting.Infrastructure.Common;
using FractalPainting.Infrastructure.UiActions;
using Ninject;
using NUnit.Framework;

namespace FractalPainting.Tests
{
    [TestFixture]
    public class Container_Should
    {
        private StandardKernel container;

        [SetUp]
        public void SetUp()
        {
            container = DIContainerTask.ConfigureContainer();
        }


        [Test]
        public void AllActionsShouldBeBound()
        {
            var expected = new[]
            {
                typeof(SaveImageAction),
                typeof(DragonFractalAction),
                typeof(KochFractalAction),
                typeof(ImageSettingsAction),
                typeof(PaletteSettingsAction)
            };
            var actualActions = container.GetAll<IUiAction>().Select(ac => ac.GetType()).ToArray();

            CollectionAssert.AreEquivalent(expected, actualActions);
        }

        [Test]
        public void PictureBoxImageHolder_ShouldBeBoundInSingletonScope()
        {
            CheckSingletonScope(typeof(PictureBoxImageHolder));
            CheckSingletonScope(typeof(IImageHolder));
        }

        [Test]
        public void PictureBoxImageHolder_ShouldBeBoundOnIImageHolderAndItself()
        {
            var imageHolder = container.Get<IImageHolder>();
            var pictureBoxImageHolder = container.Get<PictureBoxImageHolder>();
            Assert.AreSame(imageHolder, pictureBoxImageHolder);
        }

        [Test]
        public void Palette_ShouldBeBoundInSingletonScope()
        {
            CheckSingletonScope(typeof(Palette));
        }

        [Test]
        public void IDragonPainterFactory_ShouldBeBoundAsAutoGeneratedFactory()
        {
            var factoryType = GetType().Assembly.GetType("FractalPainting.App.IDragonPainterFactory");
            Assert.NotNull(factoryType, "No FractalPainting.App.IDragonPainterFactory");
            var factory = container.Get(factoryType);
            var proxyAssembly = factory.GetType().Assembly;
            if (proxyAssembly == GetType().Assembly)
                Assert.Fail("IDragonPainterFactory should be bound as factory. Do not implement it manually.");
        }

        [Test]
        public void SettingsManager_ShouldBeCreatable()
        {
            Assert.NotNull(container.Get(typeof(SettingsManager)));
        }

        [Test]
        public void AppSettings_ShouldBeLoadedFromSettingsManager()
        {
            var expected = container.Get<SettingsManager>().Load();
            var actual = container.Get<AppSettings>();
            AssertSettingEquals(expected, actual, "AppSettings should be loaded from SettingsManager");
        }

        [Test]
        public void AppSettings_ShouldBeInSingletonScope()
        {
            CheckSingletonScope(typeof(AppSettings));
        }

        [Test]
        public void ImageSettings_ShouldBeTakenFromAppSettings()
        {
            var expected = container.Get<AppSettings>().ImageSettings;
            var actual = container.Get<ImageSettings>();
            AssertImageSettingsEquals(expected, actual, "ImageSettings should be taken from AppSettings");
        }

        [Test]
        public void ImageSettings_ShouldBeInSingletonScope()
        {
            CheckSingletonScope(typeof(ImageSettings));
        }

        [Test]
        public void SecretTest_DragonPainter_ShouldStoreDependenciesInPrivateFields()
        {
            var fields = typeof(DragonPainter).GetAllFields();
            Assert.IsTrue(fields.All(field => field.IsPrivate), "All fields of DragonPainter should be private");

            var expected = new[]
            {
                typeof(IImageHolder),
                typeof(DragonSettings),
                typeof(Palette),
            };
            var actualTypes = fields.Select(field => field.FieldType);

            CollectionAssert.AreEquivalent(expected, actualTypes);
        }

        [Test]
        public void SecretTest_KochFractalAction_ShouldHaveLazyKochPainterDependency()
        {
            var types = new Type[] { typeof(Lazy<KochPainter>) };
            var ctor = typeof(KochFractalAction).GetConstructor(types);
            Assert.IsNotNull(ctor, "KochFractalAction should have a constructor with Lazy<KochPainter> type");
        }

        private void CheckSingletonScope(Type checkingType)
        {
            var obj1 = container.Get(checkingType);
            var obj2 = container.Get(checkingType);
            Assert.That(ReferenceEquals(obj1, obj2));
        }

        private static void AssertSettingEquals(AppSettings expected, AppSettings actual, string message)
        {
            Assert.NotNull(expected);
            Assert.NotNull(actual);

            AssertImageSettingsEquals(expected.ImageSettings, actual.ImageSettings, message);
            Assert.IsTrue(expected.ImagesDirectory == actual.ImagesDirectory, message);
        }

        private static void AssertImageSettingsEquals(ImageSettings expected, ImageSettings actual, string message)
        {
            Assert.AreEqual(expected.Width, actual.Width, message);
            Assert.AreEqual(expected.Height, actual.Height, message);
        }
    }
}