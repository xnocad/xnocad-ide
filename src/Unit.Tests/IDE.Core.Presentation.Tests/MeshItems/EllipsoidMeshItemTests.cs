﻿using IDE.Core.Designers;
using IDE.Core.Types.Media3D;
using Xunit;

namespace IDE.Core.Presentation.Tests.MeshItems
{
    public class EllipsoidMeshItemTests
    {
        [Theory]
        [InlineData(0, 0, 1, 1)]
        public void Translate(double x, double y, double dx, double dy)
        {
            var item = new EllipsoidMeshItem
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
            var item = new EllipsoidMeshItem
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

        [Fact]
        public void MirrorX()
        {
            var item = new EllipsoidMeshItem
            {
                X = 0,
                Y = 0,
            };

            item.MirrorX();

            //no effect
            //Assert.Equal(expectedRot, item.RotationX);
        }

        [Fact]
        public void MirrorY()
        {
            var item = new EllipsoidMeshItem
            {
                X = 0,
                Y = 0,
            };

            item.MirrorY();

            //no effect
            //Assert.Equal(expectedRot, item.RotationY);
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        public void Rotate(double x, double y,
                           double expectedX, double expectedY)
        {
            var item = new EllipsoidMeshItem
            {
                X = x,
                Y = y,
                IsPlaced = true,
            };

            item.Rotate();

            Assert.Equal(expectedX, item.X);
            Assert.Equal(expectedY, item.Y);
        }
    }
}
