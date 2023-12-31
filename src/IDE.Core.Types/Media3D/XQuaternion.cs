﻿using IDE.Core.Types.Media;
using System;
using System.Diagnostics;

namespace IDE.Core.Types.Media3D
{
    public struct XQuaternion
    {
        #region Constructors

        /// <summary>
        /// Constructor that sets XQuaternion's initial values.
        /// </summary>
        /// <param name="x">Value of the X coordinate of the new XQuaternion.</param>
        /// <param name="y">Value of the Y coordinate of the new XQuaternion.</param>
        /// <param name="z">Value of the Z coordinate of the new XQuaternion.</param>
        /// <param name="w">Value of the W coordinate of the new XQuaternion.</param>
        public XQuaternion(double x, double y, double z, double w)
        {
            _x = x;
            _y = y;
            _z = z;
            _w = w;
            _isNotDistinguishedIdentity = true;
        }

        /// <summary>
        /// Constructs a XQuaternion via specified axis of rotation and an angle.
        /// Throws an InvalidOperationException if given (0,0,0) as axis vector.
        /// </summary>
        /// <param name="axisOfRotation">Vector representing axis of rotation.</param>
        /// <param name="angleInDegrees">Angle to turn around the given axis (in degrees).</param>
        public XQuaternion(XVector3D axisOfRotation, double angleInDegrees)
        {
            angleInDegrees %= 360.0; // Doing the modulo before converting to radians reduces total error
            double angleInRadians = angleInDegrees * (Math.PI / 180.0);
            double length = axisOfRotation.Length;
            if (length == 0)
                throw new InvalidOperationException("Quaternion_ZeroAxisSpecified");
            XVector3D v = (axisOfRotation / length) * Math.Sin(0.5 * angleInRadians);
            _x = v.X;
            _y = v.Y;
            _z = v.Z;
            _w = Math.Cos(0.5 * angleInRadians);
            _isNotDistinguishedIdentity = true;
        }

        #endregion Constructors


        //------------------------------------------------------
        //
        //  Public Methods
        //
        //------------------------------------------------------

        #region Public Methods
        /// <summary>
        ///     Identity XQuaternion
        /// </summary>
        public static XQuaternion Identity
        {
            get
            {
                return s_identity;
            }
        }

        /// <summary>
        /// Retrieves XQuaternion's axis.
        /// </summary>
        public XVector3D Axis
        {
            // q = M [cos(Q/2), sin(Q /2)v]
            // axis = sin(Q/2)v
            // angle = cos(Q/2)
            // M is magnitude
            get
            {
                // Handle identity (where axis is indeterminate) by
                // returning arbitrary axis.
                if (IsDistinguishedIdentity || (_x == 0 && _y == 0 && _z == 0))
                {
                    return new XVector3D(0, 1, 0);
                }
                else
                {
                    XVector3D v = new XVector3D(_x, _y, _z);
                    v.Normalize();
                    return v;
                }
            }
        }

        /// <summary>
        /// Retrieves XQuaternion's angle.
        /// </summary>
        public double Angle
        {
            get
            {
                if (IsDistinguishedIdentity)
                {
                    return 0;
                }

                // Magnitude of XQuaternion times sine and cosine
                double msin = Math.Sqrt(_x * _x + _y * _y + _z * _z);
                double mcos = _w;

                if (!(msin <= Double.MaxValue))
                {
                    // Overflowed probably in squaring, so let's scale
                    // the values.  We don't need to include _w in the
                    // scale factor because we're not going to square
                    // it.
                    double maxcoeff = Math.Max(Math.Abs(_x), Math.Max(Math.Abs(_y), Math.Abs(_z)));
                    double x = _x / maxcoeff;
                    double y = _y / maxcoeff;
                    double z = _z / maxcoeff;
                    msin = Math.Sqrt(x * x + y * y + z * z);
                    // Scale mcos too.
                    mcos = _w / maxcoeff;
                }

                // Atan2 is better than acos.  (More precise and more efficient.)
                return Math.Atan2(msin, mcos) * (360.0 / Math.PI);
            }
        }

        /// <summary>
        /// Returns whether the XQuaternion is normalized (i.e. has a magnitude of 1).
        /// </summary>
        public bool IsNormalized
        {
            get
            {
                if (IsDistinguishedIdentity)
                {
                    return true;
                }
                double norm2 = _x * _x + _y * _y + _z * _z + _w * _w;
                return DoubleUtil.IsOne(norm2);
            }
        }

        /// <summary>
        /// Tests whether or not a given XQuaternion is an identity XQuaternion.
        /// </summary>
        public bool IsIdentity
        {
            get
            {
                return IsDistinguishedIdentity || (_x == 0 && _y == 0 && _z == 0 && _w == 1);
            }
        }

        /// <summary>
        /// Compares two XQuaternion instances for exact equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two XQuaternion instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='quaternion1'>The first XQuaternion to compare</param>
        /// <param name='quaternion2'>The second XQuaternion to compare</param>
        public static bool operator ==(XQuaternion quaternion1, XQuaternion quaternion2)
        {
            if (quaternion1.IsDistinguishedIdentity || quaternion2.IsDistinguishedIdentity)
            {
                return quaternion1.IsIdentity == quaternion2.IsIdentity;
            }
            else
            {
                return quaternion1.X == quaternion2.X &&
                       quaternion1.Y == quaternion2.Y &&
                       quaternion1.Z == quaternion2.Z &&
                       quaternion1.W == quaternion2.W;
            }
        }

        /// <summary>
        /// Compares two XQuaternion instances for exact inequality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which are logically equal may fail.
        /// Furthermore, using this equality operator, Double.NaN is not equal to itself.
        /// </summary>
        /// <returns>
        /// bool - true if the two XQuaternion instances are exactly unequal, false otherwise
        /// </returns>
        /// <param name='quaternion1'>The first XQuaternion to compare</param>
        /// <param name='quaternion2'>The second XQuaternion to compare</param>
        public static bool operator !=(XQuaternion quaternion1, XQuaternion quaternion2)
        {
            return !(quaternion1 == quaternion2);
        }
        /// <summary>
        /// Compares two XQuaternion instances for object equality.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the two XQuaternion instances are exactly equal, false otherwise
        /// </returns>
        /// <param name='quaternion1'>The first XQuaternion to compare</param>
        /// <param name='quaternion2'>The second XQuaternion to compare</param>
        public static bool Equals(XQuaternion quaternion1, XQuaternion quaternion2)
        {
            if (quaternion1.IsDistinguishedIdentity || quaternion2.IsDistinguishedIdentity)
            {
                return quaternion1.IsIdentity == quaternion2.IsIdentity;
            }
            else
            {
                return quaternion1.X.Equals(quaternion2.X) &&
                       quaternion1.Y.Equals(quaternion2.Y) &&
                       quaternion1.Z.Equals(quaternion2.Z) &&
                       quaternion1.W.Equals(quaternion2.W);
            }
        }

        /// <summary>
        /// Equals - compares this XQuaternion with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if the object is an instance of XQuaternion and if it's equal to "this".
        /// </returns>
        /// <param name='o'>The object to compare to "this"</param>
        public override bool Equals(object o)
        {
            if ((null == o) || !(o is XQuaternion))
            {
                return false;
            }

            XQuaternion value = (XQuaternion)o;
            return XQuaternion.Equals(this, value);
        }

        /// <summary>
        /// Equals - compares this XQuaternion with the passed in object.  In this equality
        /// Double.NaN is equal to itself, unlike in numeric equality.
        /// Note that double values can acquire error when operated upon, such that
        /// an exact comparison between two values which
        /// are logically equal may fail.
        /// </summary>
        /// <returns>
        /// bool - true if "value" is equal to "this".
        /// </returns>
        /// <param name='value'>The XQuaternion to compare to "this"</param>
        public bool Equals(XQuaternion value)
        {
            return XQuaternion.Equals(this, value);
        }
        /// <summary>
        /// Returns the HashCode for this XQuaternion
        /// </summary>
        /// <returns>
        /// int - the HashCode for this XQuaternion
        /// </returns>
        public override int GetHashCode()
        {
            if (IsDistinguishedIdentity)
            {
                return c_identityHashCode;
            }
            else
            {
                // Perform field-by-field XOR of HashCodes
                return X.GetHashCode() ^
                       Y.GetHashCode() ^
                       Z.GetHashCode() ^
                       W.GetHashCode();
            }
        }


        /// <summary>
        /// Relaces XQuaternion with its conjugate
        /// </summary>
        public void Conjugate()
        {
            if (IsDistinguishedIdentity)
            {
                return;
            }

            // Conjugate([x,y,z,w]) = [-x,-y,-z,w]
            _x = -_x;
            _y = -_y;
            _z = -_z;
        }

        /// <summary>
        /// Replaces XQuaternion with its inverse
        /// </summary>
        public void Invert()
        {
            if (IsDistinguishedIdentity)
            {
                return;
            }

            // Inverse = Conjugate / Norm Squared
            Conjugate();
            double norm2 = _x * _x + _y * _y + _z * _z + _w * _w;
            _x /= norm2;
            _y /= norm2;
            _z /= norm2;
            _w /= norm2;
        }

        /// <summary>
        /// Normalizes this XQuaternion.
        /// </summary>
        public void Normalize()
        {
            if (IsDistinguishedIdentity)
            {
                return;
            }

            double norm2 = _x * _x + _y * _y + _z * _z + _w * _w;
            if (norm2 > Double.MaxValue)
            {
                // Handle overflow in computation of norm2
                double rmax = 1.0 / Max(Math.Abs(_x),
                                      Math.Abs(_y),
                                      Math.Abs(_z),
                                      Math.Abs(_w));

                _x *= rmax;
                _y *= rmax;
                _z *= rmax;
                _w *= rmax;
                norm2 = _x * _x + _y * _y + _z * _z + _w * _w;
            }
            double normInverse = 1.0 / Math.Sqrt(norm2);
            _x *= normInverse;
            _y *= normInverse;
            _z *= normInverse;
            _w *= normInverse;
        }

        /// <summary>
        /// XQuaternion addition.
        /// </summary>
        /// <param name="left">First XQuaternion being added.</param>
        /// <param name="right">Second XQuaternion being added.</param>
        /// <returns>Result of addition.</returns>
        public static XQuaternion operator +(XQuaternion left, XQuaternion right)
        {
            if (right.IsDistinguishedIdentity)
            {
                if (left.IsDistinguishedIdentity)
                {
                    return new XQuaternion(0, 0, 0, 2);
                }
                else
                {
                    // We know left is not distinguished identity here.                    
                    left._w += 1;
                    return left;
                }
            }
            else if (left.IsDistinguishedIdentity)
            {
                // We know right is not distinguished identity here.
                right._w += 1;
                return right;
            }
            else
            {
                return new XQuaternion(left._x + right._x,
                                      left._y + right._y,
                                      left._z + right._z,
                                      left._w + right._w);
            }
        }

        /// <summary>
        /// XQuaternion addition.
        /// </summary>
        /// <param name="left">First XQuaternion being added.</param>
        /// <param name="right">Second XQuaternion being added.</param>
        /// <returns>Result of addition.</returns>
        public static XQuaternion Add(XQuaternion left, XQuaternion right)
        {
            return (left + right);
        }

        /// <summary>
        /// XQuaternion subtraction.
        /// </summary>
        /// <param name="left">XQuaternion to subtract from.</param>
        /// <param name="right">XQuaternion to subtract from the first XQuaternion.</param>
        /// <returns>Result of subtraction.</returns>
        public static XQuaternion operator -(XQuaternion left, XQuaternion right)
        {
            if (right.IsDistinguishedIdentity)
            {
                if (left.IsDistinguishedIdentity)
                {
                    return new XQuaternion(0, 0, 0, 0);
                }
                else
                {
                    // We know left is not distinguished identity here.
                    left._w -= 1;
                    return left;
                }
            }
            else if (left.IsDistinguishedIdentity)
            {
                // We know right is not distinguished identity here.
                return new XQuaternion(-right._x, -right._y, -right._z, 1 - right._w);
            }
            else
            {
                return new XQuaternion(left._x - right._x,
                                      left._y - right._y,
                                      left._z - right._z,
                                      left._w - right._w);
            }
        }

        /// <summary>
        /// XQuaternion subtraction.
        /// </summary>
        /// <param name="left">XQuaternion to subtract from.</param>
        /// <param name="right">XQuaternion to subtract from the first XQuaternion.</param>
        /// <returns>Result of subtraction.</returns>
        public static XQuaternion Subtract(XQuaternion left, XQuaternion right)
        {
            return (left - right);
        }

        /// <summary>
        /// XQuaternion multiplication.
        /// </summary>
        /// <param name="left">First XQuaternion.</param>
        /// <param name="right">Second XQuaternion.</param>
        /// <returns>Result of multiplication.</returns>
        public static XQuaternion operator *(XQuaternion left, XQuaternion right)
        {
            if (left.IsDistinguishedIdentity)
            {
                return right;
            }
            if (right.IsDistinguishedIdentity)
            {
                return left;
            }

            double x = left._w * right._x + left._x * right._w + left._y * right._z - left._z * right._y;
            double y = left._w * right._y + left._y * right._w + left._z * right._x - left._x * right._z;
            double z = left._w * right._z + left._z * right._w + left._x * right._y - left._y * right._x;
            double w = left._w * right._w - left._x * right._x - left._y * right._y - left._z * right._z;
            XQuaternion result = new XQuaternion(x, y, z, w);
            return result;

        }

        /// <summary>
        /// XQuaternion multiplication.
        /// </summary>
        /// <param name="left">First XQuaternion.</param>
        /// <param name="right">Second XQuaternion.</param>
        /// <returns>Result of multiplication.</returns>
        public static XQuaternion Multiply(XQuaternion left, XQuaternion right)
        {
            return left * right;
        }

        /// <summary>
        /// Scale this XQuaternion by a scalar.
        /// </summary>
        /// <param name="scale">Value to scale by.</param>            
        private void Scale(double scale)
        {
            if (IsDistinguishedIdentity)
            {
                _w = scale;
                IsDistinguishedIdentity = false;
                return;
            }
            _x *= scale;
            _y *= scale;
            _z *= scale;
            _w *= scale;
        }

        /// <summary>
        /// Return length of XQuaternion.
        /// </summary>
        private double Length()
        {
            if (IsDistinguishedIdentity)
            {
                return 1;
            }

            double norm2 = _x * _x + _y * _y + _z * _z + _w * _w;
            if (!(norm2 <= Double.MaxValue))
            {
                // Do this the slow way to avoid squaring large
                // numbers since the length of many quaternions is
                // representable even if the squared length isn't.  Of
                // course some lengths aren't representable because
                // the length can be up to twice as big as the largest
                // coefficient.

                double max = Math.Max(Math.Max(Math.Abs(_x), Math.Abs(_y)),
                                      Math.Max(Math.Abs(_z), Math.Abs(_w)));

                double x = _x / max;
                double y = _y / max;
                double z = _z / max;
                double w = _w / max;

                double smallLength = Math.Sqrt(x * x + y * y + z * z + w * w);
                // Return length of this smaller vector times the scale we applied originally.
                return smallLength * max;
            }
            return Math.Sqrt(norm2);
        }

        /// <summary>
        /// Smoothly interpolate between the two given quaternions using Spherical 
        /// Linear Interpolation (SLERP).
        /// </summary>
        /// <param name="from">First XQuaternion for interpolation.</param>
        /// <param name="to">Second XQuaternion for interpolation.</param>
        /// <param name="t">Interpolation coefficient.</param>
        /// <returns>SLERP-interpolated XQuaternion between the two given quaternions.</returns>
        public static XQuaternion Slerp(XQuaternion from, XQuaternion to, double t)
        {
            return Slerp(from, to, t, /* useShortestPath = */ true);
        }

        /// <summary>
        /// Smoothly interpolate between the two given quaternions using Spherical 
        /// Linear Interpolation (SLERP).
        /// </summary>
        /// <param name="from">First XQuaternion for interpolation.</param>
        /// <param name="to">Second XQuaternion for interpolation.</param>
        /// <param name="t">Interpolation coefficient.</param>
        /// <param name="useShortestPath">If true, Slerp will automatically flip the sign of
        ///     the destination XQuaternion to ensure the shortest path is taken.</param>
        /// <returns>SLERP-interpolated XQuaternion between the two given quaternions.</returns>
        public static XQuaternion Slerp(XQuaternion from, XQuaternion to, double t, bool useShortestPath)
        {
            if (from.IsDistinguishedIdentity)
            {
                from._w = 1;
            }
            if (to.IsDistinguishedIdentity)
            {
                to._w = 1;
            }

            double cosOmega;
            double scaleFrom, scaleTo;

            // Normalize inputs and stash their lengths
            double lengthFrom = from.Length();
            double lengthTo = to.Length();
            from.Scale(1 / lengthFrom);
            to.Scale(1 / lengthTo);

            // Calculate cos of omega.
            cosOmega = from._x * to._x + from._y * to._y + from._z * to._z + from._w * to._w;

            if (useShortestPath)
            {
                // If we are taking the shortest path we flip the signs to ensure that
                // cosOmega will be positive.
                if (cosOmega < 0.0)
                {
                    cosOmega = -cosOmega;
                    to._x = -to._x;
                    to._y = -to._y;
                    to._z = -to._z;
                    to._w = -to._w;
                }
            }
            else
            {
                // If we are not taking the UseShortestPath we clamp cosOmega to
                // -1 to stay in the domain of Math.Acos below.
                if (cosOmega < -1.0)
                {
                    cosOmega = -1.0;
                }
            }

            // Clamp cosOmega to [-1,1] to stay in the domain of Math.Acos below.
            // The logic above has either flipped the sign of cosOmega to ensure it
            // is positive or clamped to -1 aready.  We only need to worry about the
            // upper limit here.
            if (cosOmega > 1.0)
            {
                cosOmega = 1.0;
            }

            Debug.Assert(!(cosOmega < -1.0) && !(cosOmega > 1.0),
                "cosOmega should be clamped to [-1,1]");

            // The mainline algorithm doesn't work for extreme
            // cosine values.  For large cosine we have a better
            // fallback hence the asymmetric limits.
            const double maxCosine = 1.0 - 1e-6;
            const double minCosine = 1e-10 - 1.0;

            // Calculate scaling coefficients.
            if (cosOmega > maxCosine)
            {
                // Quaternions are too close - use linear interpolation.
                scaleFrom = 1.0 - t;
                scaleTo = t;
            }
            else if (cosOmega < minCosine)
            {
                // Quaternions are nearly opposite, so we will pretend to 
                // is exactly -from.
                // First assign arbitrary perpendicular to "to".
                to = new XQuaternion(-from.Y, from.X, -from.W, from.Z);

                double theta = t * Math.PI;

                scaleFrom = Math.Cos(theta);
                scaleTo = Math.Sin(theta);
            }
            else
            {
                // Standard case - use SLERP interpolation.
                double omega = Math.Acos(cosOmega);
                double sinOmega = Math.Sqrt(1.0 - cosOmega * cosOmega);
                scaleFrom = Math.Sin((1.0 - t) * omega) / sinOmega;
                scaleTo = Math.Sin(t * omega) / sinOmega;
            }

            // We want the magnitude of the output XQuaternion to be
            // multiplicatively interpolated between the input
            // magnitudes, i.e. lengthOut = lengthFrom * (lengthTo/lengthFrom)^t
            //                            = lengthFrom ^ (1-t) * lengthTo ^ t

            double lengthOut = lengthFrom * Math.Pow(lengthTo / lengthFrom, t);
            scaleFrom *= lengthOut;
            scaleTo *= lengthOut;

            return new XQuaternion(scaleFrom * from._x + scaleTo * to._x,
                                  scaleFrom * from._y + scaleTo * to._y,
                                  scaleFrom * from._z + scaleTo * to._z,
                                  scaleFrom * from._w + scaleTo * to._w);
        }

        #endregion Public Methods

        #region Private Methods

        static private double Max(double a, double b, double c, double d)
        {
            if (b > a)
                a = b;
            if (c > a)
                a = c;
            if (d > a)
                a = d;
            return a;
        }

        #endregion Private Methods

        //------------------------------------------------------
        //
        //  Public Properties
        //
        //------------------------------------------------------

        #region Public Properties

        /// <summary>
        /// X - Default value is 0.
        /// </summary>
        public double X
        {
            get
            {
                return _x;
            }

            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _x = value;
            }
        }

        /// <summary>
        /// Y - Default value is 0.
        /// </summary>
        public double Y
        {
            get
            {
                return _y;
            }

            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _y = value;
            }
        }

        /// <summary>
        /// Z - Default value is 0.
        /// </summary>
        public double Z
        {
            get
            {
                return _z;
            }

            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _z = value;
            }
        }

        /// <summary>
        /// W - Default value is 1.
        /// </summary>
        public double W
        {
            get
            {
                if (IsDistinguishedIdentity)
                {
                    return 1.0;
                }
                else
                {
                    return _w;
                }
            }

            set
            {
                if (IsDistinguishedIdentity)
                {
                    this = s_identity;
                    IsDistinguishedIdentity = false;
                }
                _w = value;
            }
        }

        #endregion Public Properties

        //------------------------------------------------------
        //
        //  Internal Fields
        //
        //------------------------------------------------------

        #region Internal Fields

        internal double _x;
        internal double _y;
        internal double _z;
        internal double _w;

        #endregion Internal Fields

        #region Private Fields and Properties

        // If this bool is false then we are a default XQuaternion with
        // all doubles equal to zero, but should be treated as
        // identity.
        private bool _isNotDistinguishedIdentity;

        private bool IsDistinguishedIdentity
        {
            get
            {
                return !_isNotDistinguishedIdentity;
            }
            set
            {
                _isNotDistinguishedIdentity = !value;
            }
        }

        private static int GetIdentityHashCode()
        {
            // This code is called only once.
            double zero = 0;
            double one = 1;
            // return zero.GetHashCode() ^ zero.GetHashCode() ^ zero.GetHashCode() ^ one.GetHashCode();
            // But this expression can be simplified because the first two hash codes cancel.
            return zero.GetHashCode() ^ one.GetHashCode();
        }

        private static XQuaternion GetIdentity()
        {
            // This code is called only once.
            XQuaternion q = new XQuaternion(0, 0, 0, 1);
            q.IsDistinguishedIdentity = true;
            return q;
        }


        // Hash code for identity.
        private static int c_identityHashCode = GetIdentityHashCode();

        // Default identity
        private static XQuaternion s_identity = GetIdentity();

        #endregion Private Fields and Properties
    }
}
