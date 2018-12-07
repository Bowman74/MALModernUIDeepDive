﻿using System;
using UIKit;
using ARKit;
using Foundation;
using MalModernUi;
using MalModernUi.iOS.Renderers;
using SceneKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;
using OpenTK;

[assembly: ExportRenderer(typeof(ArView), typeof(ArViewRenderer))]
namespace MalModernUi.iOS.Renderers
{
    public class ArViewRenderer : PageRenderer, IARSCNViewDelegate
    {
        private ARSCNView sceneView;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            sceneView = new ARSCNView
            {
                Delegate = this
            };
            this.View = sceneView;

            sceneView.ShowsStatistics = true;

            var tapGestureRecognizer = new UITapGestureRecognizer(HandleTap);
            sceneView.AddGestureRecognizer(tapGestureRecognizer);
        }

        private void HandleTap(UITapGestureRecognizer recognizer)
        {
            var tapLocation = recognizer.LocationInView(sceneView);
            var hitTestResults = sceneView.HitTest(tapLocation, ARHitTestResultType.ExistingPlaneUsingExtent);

            if (hitTestResults != null && hitTestResults.Length > 0)
            {
                var hitTestResult = hitTestResults[0];
                var translation = PositionFromTransform(hitTestResult.WorldTransform);

                var x = translation.X;
                var y = translation.Y;
                var z = translation.Z;

                var shipScene = SCNScene.FromFile("Sprites/ship.scn");
                var shipNode = shipScene.RootNode.FindChildNode("ship", false);

                shipNode.Position = new SCNVector3(x, y, z);
                sceneView.Scene.RootNode.AddChildNode(shipNode);
            }
        }

        private SCNVector3 PositionFromTransform(NMatrix4 xform)
        {
            return new SCNVector3(xform.M14, xform.M24, xform.M34);
        }


        public override void ViewDidUnload()
        {
            sceneView.RemoveFromSuperview();
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);

            var configuration = new ARWorldTrackingConfiguration
            {
                PlaneDetection = (ARPlaneDetection.Horizontal | ARPlaneDetection.Vertical),
                AutoFocusEnabled = true
            };

            // Run the view's session
            sceneView.Session.Run(configuration);
        }

        public override void ViewWillDisappear(bool animated)
        {
            base.ViewWillDisappear(animated);
            sceneView.Session.Pause();
        }

        [Export("renderer:didUpdateNode:forAnchor:")]
        public void DidUpdateNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            var planeAnchor = anchor as ARPlaneAnchor;
            if (planeAnchor == null) return;

            Console.WriteLine($"didUpdateNode :{node.ChildNodes.Length}");

            foreach (var childNode in node.ChildNodes) 
            {
                SCNPlane planeNode = childNode.Geometry as SCNPlane;
                if (planeNode != null)
                {
                    childNode.Position = new SCNVector3(planeAnchor.Center.X, 0f, planeAnchor.Center.Z);
                    planeNode.Width = new nfloat(planeAnchor.Extent.X);
                    planeNode.Height = new nfloat(planeAnchor.Extent.Z);
                } 
                else if (childNode.Geometry is SCNText) 
                {
                    childNode.Position = new SCNVector3(-0.3f, -0.55f, 0.25f);
                }
            }
        }

        [Export("renderer:didAddNode:forAnchor:")]
        public void DidAddNode(ISCNSceneRenderer renderer, SCNNode node, ARAnchor anchor)
        {
            var planeAnchor = anchor as ARPlaneAnchor;
            if (planeAnchor == null) return;

            Console.WriteLine("didAddNode");

            var text = SCNText.Create("This is a good spot", 0);
            text.Font = UIFont.FromName("Arial", 1);
            if (text.FirstMaterial != null)
            {
                text.FirstMaterial.Diffuse.Contents = UIColor.Green;
            }
            
            var textNode = SCNNode.FromGeometry(text);
            textNode.Position = new SCNVector3(-0.3f, -0.55f, 0.25f);
            textNode.Scale = new SCNVector3(0.075f, 0.1f, 0.5f);

            var plane = SCNPlane.Create(new nfloat(planeAnchor.Extent.X), new nfloat(planeAnchor.Extent.Z));
            var planeNode = SCNNode.FromGeometry(plane);
            planeNode.Position = new SCNVector3(planeAnchor.Center.X, 0f, planeAnchor.Center.Z);

            var txtAngles = textNode.EulerAngles;
            txtAngles.X = (float)(-1f * (Math.PI / 2f));
            textNode.EulerAngles = txtAngles;
            var planeAngles = planeNode.EulerAngles;
            planeAngles.X = (float)(-1f * (Math.PI / 2f));
            planeNode.EulerAngles = planeAngles;

            planeNode.Opacity = 0.25f;

            node.AddChildNode(planeNode);
            node.AddChildNode(textNode);
        }
    }
}