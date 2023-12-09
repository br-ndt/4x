﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Tile : MonoBehaviour
{
    public float stepHeight = 0f;

    public virtual Vector3 Center
    {
        get
        {
            return _center;
        }
    }
    protected Vector3 _center;

    public virtual Point Position
    {
        get
        {
            return _pos;
        }
        set
        {
            _pos = value;
            _center = new Vector3(value.x, _height * stepHeight, value.y);
            transform.localPosition = _center;
        }
    }
    protected Point _pos;

    public virtual float Height
    {
        get
        {
            return _height;
        }
        set
        {
            _height = value;
        }
    }
    protected float _height;

    public TerrainType Terrain
    {
        get
        {
            return _terrain;
        }
        set
        {
            _terrain = value;
            gameObject.name = $"{_terrain.name} ({_pos.x}, {_pos.y})";
            GetComponent<Renderer>().material = value.material;
        }
    }
    protected TerrainType _terrain;

    public virtual void Grow()
    {
        Height += 0.1f;
    }

    public virtual void Shrink()
    {
        Height -= 0.1f;
    }
}
