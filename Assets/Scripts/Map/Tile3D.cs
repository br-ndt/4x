using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Tile3D : Tile
{
    public override float Height
    {
        set
        {
            base.Height = value;
            transform.localPosition = new Vector3(_pos.x, ((_height + 1) * stepHeight / 2f) + stepHeight, _pos.y);
            transform.localScale = new Vector3(1, ((_height + 1) * stepHeight) + stepHeight / 2f, 1);
            _center = new Vector3(_pos.x, transform.position.y + transform.localScale.y / 2, _pos.y);
        }
    }
}
