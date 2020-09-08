    public Polyline[] MakeStraightSkeleton(Brep brep)
    {
        // This must be performed near the origin and scaled up...

        // Move to origin:
        var duplicate = brep.DuplicateBrep();
        var vertices = brep.DuplicateVertices();
        var vector = new Vector3d(new BoundingBox(vertices).Center);

        var translateFromOrigin = Transform.Translation(vector).Clone();
        vector.Reverse();

        var translateToOrigin = Transform.Translation(vector).Clone();
        duplicate.Transform(translateToOrigin);

        var scaleFactor = 1e4;
        var scaleUp = Transform.Scale(Plane.WorldXY.Origin, scaleFactor);
        var scaleDown = Transform.Scale(Plane.WorldXY.Origin, 1 / scaleFactor);
        duplicate.Transform(scaleUp);

        //Perform StraighSkeleton:
        var innerLoops = new List<Polyline>();
        Polyline boundary = null;
        foreach (BrepLoop loop in duplicate.Loops)
        {
            Polyline loopPolyline;
            var loopCurve = loop.To3dCurve().TryGetPolyline(out loopPolyline);
            if (loop.LoopType == BrepLoopType.Outer)
            {
                boundary = loopPolyline;
            }

            else
            {
                innerLoops.Add(loopPolyline);
            }
        }

        var sskOuterLoop = new List<SskVector2d>();
        foreach (Point3d pt in boundary)
        {
            var sskVect = new SskVector2d(pt.X, pt.Y);
            sskOuterLoop.Add(sskVect);
        }
        sskOuterLoop.RemoveAt(sskOuterLoop.Count - 1);

        var sskInnerLoops = new List<List<SskVector2d>>();
        foreach (Polyline innerLoop in innerLoops)
        {
            var sskInnerLoop = new List<SskVector2d>();
            foreach (Point3d pt in innerLoop)
            {
                var sskVect = new SskVector2d(pt.X, pt.Y);
                sskInnerLoop.Add(sskVect);
            }
            sskInnerLoop.RemoveAt(sskInnerLoop.Count - 1);
            sskInnerLoops.Add(sskInnerLoop);
        }

        var sk = SkeletonBuilder.Build(sskOuterLoop, sskInnerLoops);

        var lineList = new List<Line>();
        var dupPtList = new List<Point3d>();

        var faces = new List<Polyline>();

        foreach (EdgeResult edge in sk.Edges)
        {
            List<SskVector2d> points = edge.Polygon;
            var facePtList = new List<Point3d>();

            foreach (SskVector2d sskVector2d in points)
            {
                var rhinoPt = new Point3d(sskVector2d.X, sskVector2d.Y, 0);

                // Check if rhinoPt == input Polygon pt:
                int dupCount = 0;
                for (int i = 0; i < boundary.Count; i++)
                {
                    if (rhinoPt.X == boundary[i].X && rhinoPt.Y == boundary[i].Y && rhinoPt.Z == boundary[i].Z)
                    {
                        dupCount = dupCount + 1;
                    }
                    if (dupCount > 1)
                    {
                        dupPtList.Add(rhinoPt);
                    }
                }

                facePtList.Add(rhinoPt);
            }
            var startPt = facePtList[0];
            facePtList.Add(startPt);
            Polyline face = new Polyline(facePtList);


            // Move back:
            face.Transform(scaleDown);
            face.Transform(translateFromOrigin);
            faces.Add(face.Duplicate());
        }

        return faces.ToArray();

    }