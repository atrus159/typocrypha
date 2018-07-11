﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using NodeEditorFramework;

namespace TypocryphaGameflow
{
    [Node(false, "Gameflow/Start", new System.Type[] { typeof(GameflowCanvas) })]
    public class GameflowStartNode : Node
    {
        public const string ID = "Gameflow Start Node";
        public override string GetID { get { return ID; } }

        public override string Title { get { return "Gameflow Start"; } }
        public override Vector2 MinSize { get { return new Vector2(150, 60); } }
        public override bool AutoLayout { get { return true; } }

        //Next Node to go to (OUTPUT)
        [ConnectionKnob("To Next", Direction.Out, "Gameflow", NodeSide.Right, 30)]
        public ConnectionKnob toNextOUT;

        public override void NodeGUI()
        {
            GUILayout.Space(3);
        }
    }
}