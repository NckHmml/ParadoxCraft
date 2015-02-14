using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.EntityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParadoxCraft
{
    public class Movement
    {
        private Entity Camera { get; set; }
        private CameraComponent Component
        {
            get
            {
                return Camera.Get<CameraComponent>();
            }
        }
        private float Speed { get; set; }
        private float YawAngle { get; set; }
        private float PitchAngle { get; set; }

        private Direction MoveDirection { get; set; }

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public Vector3 Position
        {
            get
            {
                return Camera.Transformation.Translation;
            }
        }

        public Movement(Entity camera)
        {
            Camera = camera;
            Speed = 10f;
            YawAngle = 0;
            PitchAngle = 0;
            X = camera.Transformation.Translation.X;
            Y = camera.Transformation.Translation.Y;
            Z = camera.Transformation.Translation.Z;
        }

        public void Add(Direction direction)
        {
            MoveDirection |= direction;
        }

        public void Move(double multi)
        {
            if (MoveDirection == 0) return;

            bool
                foward = (MoveDirection & Direction.Foward) != 0,
                back = (MoveDirection & Direction.Back) != 0,
                left = (MoveDirection & Direction.Left) != 0,
                right = (MoveDirection & Direction.Right) != 0,
                up = (MoveDirection & Direction.Up) != 0,
                down = (MoveDirection & Direction.Down) != 0;

            if (left && foward)
            {
                X -= Sin(YawAngle + (Constants.degrees90 / 2), multi);
                Z -= Cos(YawAngle + (Constants.degrees90 / 2), multi);
            }
            else if (right && foward)
            {
                X -= Sin(YawAngle - (Constants.degrees90 / 2), multi);
                Z -= Cos(YawAngle - (Constants.degrees90 / 2), multi);
            }
            else if (left && back)
            {
                X += Sin(YawAngle - (Constants.degrees90 / 2), multi);
                Z += Cos(YawAngle - (Constants.degrees90 / 2), multi);
            }
            else if (right && back)
            {
                X += Sin(YawAngle + (Constants.degrees90 / 2), multi);
                Z += Cos(YawAngle + (Constants.degrees90 / 2), multi);
            }
            else if (left)
            {
                X -= Sin(YawAngle + Constants.degrees90, multi);
                Z -= Cos(YawAngle + Constants.degrees90, multi);
            }
            else if (right)
            {
                X -= Sin(YawAngle - Constants.degrees90, multi);
                Z -= Cos(YawAngle - Constants.degrees90, multi);
            }
            else if (back)
            {
                Z += Cos(YawAngle, multi);
                X += Sin(YawAngle, multi);
            }
            else if (foward)
            {
                Z -= Cos(YawAngle, multi);
                X -= Sin(YawAngle, multi);
            }

            if (up)
            {
                Y += Speed * multi;
            }
            else if (down)
            {
                Y -= Speed * multi;
            }

            MoveDirection = 0;
            SetPosition();
        }

        private double Sin(float angle, double multi)
        {
            return Speed * multi * Math.Sin(angle);
        }

        private double Cos(float angle, double multi)
        {
            return Speed * multi * Math.Cos(angle);
        }

        public void YawPitch(float yaw, float pitch)
        {
            YawAngle += yaw * 5f;
            YawAngle %= Constants.degrees90 * 4;

            PitchAngle += pitch * 5f;
            if (PitchAngle > Constants.degrees90) //Prevent backflip
                PitchAngle = Constants.degrees90;
            if (PitchAngle < -Constants.degrees90) //Prevent frontflip
                PitchAngle = -Constants.degrees90;

            Camera.Transformation.Rotation = Quaternion.RotationYawPitchRoll(YawAngle, PitchAngle, 0);
        }

        public bool isInField(Vector3 position)
        {
            var farplane = Camera.Get<CameraComponent>().FarPlane;

            var height = X - position.X;
            var width = Z - position.Z;
            var distance = Math.Sqrt((height * height) + (width * width));
            return distance <= farplane * 1.3f;
        }

        public bool isAhead(Vector3 position)
        {
            var curPos = Camera.Transformation.Translation;
            var height = curPos.X - position.X;
            var width = curPos.Z - position.Z;

            var angle = Math.Tan(height / width);

            return angle < Constants.degrees90;
        }

        private void SetPosition()
        {
            Camera.Transformation.Translation.X = (float)X;
            Camera.Transformation.Translation.Y = (float)Y;
            Camera.Transformation.Translation.Z = (float)Z;
        }
    }

    [Flags]
    public enum Direction
    {
        Foward = 1,
        Back = 2,

        Left = 4,
        Right = 8,

        Up = 16,
        Down = 32
    }
}
