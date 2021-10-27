using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BhorGames.Mechanics
{
    public class SwipeMovement
    {
        public enum Axis
        {
            x, y, z
        }
        Vector3 movementMagnitude = -Vector3.one;
        public void SwipeMove(Transform _transform, Axis axis = Axis.x, float speed = 1)
        {
            switch (axis)
            {
                case Axis.x:
                    _transform.Translate(new Vector3(GetMovementMagnitude(Axis.x) * speed, 0, 0), Space.World);
                    break;
                case Axis.y:
                    _transform.Translate(new Vector3(0, GetMovementMagnitude(Axis.y) * speed, 0), Space.World);
                    break;
                case Axis.z:
                    _transform.Translate(new Vector3(0, 0, GetMovementMagnitude(Axis.z) * speed), Space.World);
                    break;
                default:
                    break;
            }
        }
        public float GetMovementMagnitude(Axis axis = Axis.x, bool onMouseHold = true)
        {
            if (!Input.GetMouseButton(0))
            {
                movementMagnitude = -Vector3.one;
                return 0;
            }
            float mag = 0;
            if (movementMagnitude == -Vector3.one)
            {
                movementMagnitude = Input.mousePosition;
            }
            movementMagnitude = Input.mousePosition - movementMagnitude;
            movementMagnitude /= 1000;
            switch (axis)
            {
                case Axis.x:
                    mag = movementMagnitude.x;
                    break;
                case Axis.y:
                    mag = movementMagnitude.y;
                    break;
                case Axis.z:
                    mag = movementMagnitude.z;
                    break;
                default:
                    break;
            }
            movementMagnitude = Input.mousePosition;
            return mag;
        }
    }
}