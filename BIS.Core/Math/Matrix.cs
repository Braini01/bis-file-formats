using BIS.Core.Streams;

namespace BIS.Core.Math
{
    /// <summary>
    /// Layout:
    /// [m00, m01, m02]
    /// [m10, m11, m12]
    /// [m20, m21, m22]
    /// </summary>
    public class Matrix3P
    {
        private Vector3P[] columns;

        public Vector3P Aside
        {
            get
            {
                return columns[0];
            }
        }
        public Vector3P Up
        {
            get
            {
                return columns[1];
            }
        }
        public Vector3P Dir
        {
            get
            {
                return columns[2];
            }
        }

        public Vector3P this[int col]
        {
            get
            {
                return columns[col];
            }
        }

        public float this[int row, int col]
        {
            get
            {
                return this[col][row];
            }

            set
            {
                this[col][row] = value;
            }
        }

        public Matrix3P() : this(0f) { }

        public Matrix3P(float val) : this(new Vector3P(val), new Vector3P(val), new Vector3P(val)) { }

        public Matrix3P(BinaryReaderEx input)
            : this(new Vector3P(input), new Vector3P(input), new Vector3P(input)) { }

        private Matrix3P(Vector3P aside, Vector3P up, Vector3P dir)
        {
            columns = new Vector3P[3]
            {
                aside,
                up,
                dir
            };
        }

        public static Matrix3P operator -(Matrix3P a)
        {
            return new Matrix3P(-a.Aside, -a.Up, -a.Dir);
        }

        public static Matrix3P operator *(Matrix3P a, Matrix3P b)
        {
            var res = new Matrix3P();

            float x, y, z;
            x = b[0, 0];
            y = b[1, 0];
            z = b[2, 0];
            res[0, 0] = a[0, 0] * x + a[0, 1] * y + a[0, 2] * z;
            res[1, 0] = a[1, 0] * x + a[1, 1] * y + a[1, 2] * z;
            res[2, 0] = a[2, 0] * x + a[2, 1] * y + a[2, 2] * z;

            x = b[0, 1];
            y = b[1, 1];
            z = b[2, 1];
            res[0, 1] = a[0,0] * x + a[0, 1] * y + a[0, 2] * z;
            res[1, 1] = a[1,0] * x + a[1, 1] * y + a[1, 2] * z;
            res[2, 1] = a[2,0] * x + a[2, 1] * y + a[2, 2] * z;

            x = b[0, 2];
            y = b[1, 2];
            z = b[2, 2];
            res[0, 2] = a[0, 0] * x + a[0, 1] * y + a[0, 2] * z;
            res[1, 2] = a[1, 0] * x + a[1, 1] * y + a[1, 2] * z;
            res[2, 2] = a[2, 0] * x + a[2, 1] * y + a[2, 2] * z;

            return res;
        }

        public void setTilda(Vector3P a)
        {
            Aside.Y = -a.Z;
            Aside.Z = a.Y;
            Up.X = a.Z;
            Up.Z = -a.X;
            Dir.X = -a.Y;
            Dir.Y = a.X;
        }

        public void write(BinaryWriterEx output)
        {
            Aside.Write(output);
            Up.Write(output);
            Dir.Write(output);
        }

        public override string ToString()
        {
            return 
$@"{this[0,0]}, {this[0, 1]}, {this[0, 2]},
{this[1, 0]}, {this[1, 1]}, {this[1, 2]},
{this[2, 0]}, {this[2, 1]}, {this[2, 2]}";
        }
    }

    /// <summary>
    /// Layout:
    /// [m00, m01, m02, m03]
    /// [m10, m11, m12, m13]
    /// [m20, m21, m22, m23]
    /// [ 0 , 0  , 0  , 1  ]
    /// </summary>

    public class Matrix4P
    {
        private Matrix3P orientation;
        private Vector3P position;

        public Matrix3P Orientation {  get { return orientation; } }
        public Vector3P Position { get { return position; } }

        public float this[int row, int col]
        {
            get
            {
                return (col == 3) ? position[row] : orientation[col][row];
            }

            set
            {
                if (col == 3)
                    position[row] = value;
                else
                    orientation[col][row] = value;
            }
        }

        public Matrix4P() : this(0f) { }
        public Matrix4P(float val) : this(new Matrix3P(val), new Vector3P(val)) { }

        public Matrix4P(BinaryReaderEx input) : this(new Matrix3P(input), new Vector3P(input)) { }

        private Matrix4P(Matrix3P orientation, Vector3P position)
        {
            this.orientation = orientation;
            this.position = position;
        }

        public static Matrix4P operator *(Matrix4P a, Matrix4P b)
        {
            var res = new Matrix4P();

            float x, y, z;
            x = b[0, 0];
            y = b[1, 0];
            z = b[2, 0];
            res[0, 0] = a[0, 0] * x + a[0, 1] * y + a[0, 2] * z;
            res[1, 0] = a[1, 0] * x + a[1, 1] * y + a[1, 2] * z;
            res[2, 0] = a[2, 0] * x + a[2, 1] * y + a[2, 2] * z;

            x = b[0, 1];
            y = b[1, 1];
            z = b[2, 1];
            res[0, 1] = a[0, 0] * x + a[0, 1] * y + a[0, 2] * z;
            res[1, 1] = a[1, 0] * x + a[1, 1] * y + a[1, 2] * z;
            res[2, 1] = a[2, 0] * x + a[2, 1] * y + a[2, 2] * z;

            x = b[0, 2];
            y = b[1, 2];
            z = b[2, 2];
            res[0, 2] = a[0, 0] * x + a[0, 1] * y + a[0, 2] * z;
            res[1, 2] = a[1, 0] * x + a[1, 1] * y + a[1, 2] * z;
            res[2, 2] = a[2, 0] * x + a[2, 1] * y + a[2, 2] * z;

            x = b.Position.X;
            y = b.Position.Y;
            z = b.Position.Z;
            res.Position.X = a[0, 0] * x + a[0, 1] * y + a[0, 2] * z + a.Position.X;
            res.Position.Y = a[1, 0] * x + a[1, 1] * y + a[1, 2] * z + a.Position.Y;
            res.Position.Z = a[2, 0] * x + a[2, 1] * y + a[2, 2] * z + a.Position.Z;

            return res;
        }

        public void write(BinaryWriterEx output)
        {
            orientation.write(output);
            position.Write(output);
        }

        public override string ToString()
        {
            return
$@"{this[0, 0]}, {this[0, 1]}, {this[0, 2]}, {this[0, 3]},
{this[1, 0]}, {this[1, 1]}, {this[1, 2]}, {this[1, 3]},
{this[2, 0]}, {this[2, 1]}, {this[2, 2]}, {this[2, 3]},
{this[3, 0]}, {this[3, 1]}, {this[3, 2]}, 1";
        }
    }
}
