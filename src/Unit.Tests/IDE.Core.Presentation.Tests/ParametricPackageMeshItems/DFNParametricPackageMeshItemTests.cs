﻿using IDE.Core.Designers;
using IDE.Core.Interfaces;
using IDE.Core.Types.Media3D;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace IDE.Core.Presentation.Tests.ParametricPackageMeshItems
{
    public class DFNParametricPackageMeshItemTests
    {
        [Theory]
        [InlineData(0, 0, 1, 1)]
        public void Translate(double x, double y, double dx, double dy)
        {
            var item = new DFNParametricPackageMeshItem
            {
                X = x,
                Y = y,
            };

            item.Translate(dx, dy, 0);

            Assert.Equal(dx, item.X - x);
            Assert.Equal(dy, item.Y - y);
        }

        [Theory]
        //translate only
        [InlineData(0, 0, 0, 1, 1, 1, 1)]
        public void TransformBy(double x, double y,
                                double rot, double tx, double ty,
                                double expectedX, double expectedY)
        {
            var item = new DFNParametricPackageMeshItem
            {
                X = x,
                Y = y,
            };

            var tg = new XTransform3DGroup();

            var rotateTransform = new XRotateTransform3D
            {
                Rotation = new XAxisAngleRotation3D { Angle = rot, Axis = new XVector3D(0, 0, 1) }
            };
            tg.Children.Add(rotateTransform);

            tg.Children.Add(new XTranslateTransform3D(tx, ty, 0));

            item.TransformBy(tg.Value);

            Assert.Equal(expectedX, item.X);
            Assert.Equal(expectedY, item.Y);
        }

        [Theory]
        [InlineData(0, 180)]
        [InlineData(180, 0)]
        [InlineData(90, 270)]
        public void MirrorX(double rot, double expectedRot)
        {
            var item = new DFNParametricPackageMeshItem
            {
                X = 0,
                Y = 0,
                RotationX = rot,
            };

            item.MirrorX();

            Assert.Equal(expectedRot, item.RotationX);
        }

        [Theory]
        [InlineData(0, 180)]
        [InlineData(180, 0)]
        [InlineData(90, 270)]
        public void MirrorY(double rot, double expectedRot)
        {
            var item = new DFNParametricPackageMeshItem
            {
                X = 0,
                Y = 0,
                RotationY = rot
            };

            item.MirrorY();

            Assert.Equal(expectedRot, item.RotationY);
        }

        [Theory]
        [InlineData(0, 0, 0, 0, 0, 90)]
        [InlineData(0, 0, 90, 0, 0, 180)]
        [InlineData(0, 0, 180, 0, 0, 270)]
        [InlineData(0, 0, 270, 0, 0, 0)]
        public void Rotate(double x, double y, double rot,
                           double expectedX, double expectedY, double expectedRot)
        {
            var item = new DFNParametricPackageMeshItem
            {
                X = x,
                Y = y,
                RotationZ = rot,
                IsPlaced = true,
            };

            item.Rotate();

            Assert.Equal(expectedX, item.X);
            Assert.Equal(expectedY, item.Y);
            Assert.Equal(expectedRot, item.RotationZ);
        }

        [Fact]
        public async Task GenerateItems()
        {
            var meshHelperMock = new Mock<IMeshHelper>();
            var dispatcherMock = new Mock<IDispatcherHelper>();


            ServiceProvider.RegisterResolver(t =>
            {
                if (t == typeof(IMeshHelper))
                    return meshHelperMock.Object;
                if (t == typeof(IDispatcherHelper))
                    return dispatcherMock.Object;

                throw new NotImplementedException();
            });

            var item = new DFNParametricPackageMeshItem();

            await item.GenerateItems();

            Assert.NotNull(item.Items);
            Assert.True(item.Items.Count > 0);
        }
    }

}
