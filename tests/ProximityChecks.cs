using ParkingTrainer;

internal static class ProximityChecks
{
    public static void Run(Action<bool,string> check)
    {
        double Reading(Pose pose,Obstacle obstacle)=>Enumerable.Range(0,6).Min(i=>ParkingProximity.Measure(pose,new[]{obstacle},i<3,i%3-1));
        Obstacle Local(Pose pose,double x,double z,double width=.02,double length=.02,double yaw=0)
        {var p=pose.Point(x,z);return new(new(p.X,p.Z,width,length,pose.Yaw+yaw),"test");}
        foreach(double yaw in new[]{0,.37,1.2,Math.PI})
        {
            var pose=new Pose(7,-4,yaw);
            foreach(int side in new[]{-1,1})
            {
                for(int step=0;step<=90;step++)
                {
                    double z=-CarSpec.Front+CarSpec.Length*step/90;
                    var post=Local(pose,side*1.01,z);
                    check(Reading(pose,post)<.101,"Thin side post remains detected along the entire body");
                    check(Math.Min(ParkingProximity.Measure(pose,new[]{post},true,side),ParkingProximity.Measure(pose,new[]{post},false,side))<.101,"Side protection reports the correct side");
                }
                foreach(bool front in new[]{false,true})
                {
                    var post=Local(pose,side*1.01,front?-CarSpec.Front-.11:CarSpec.Rear+.11);
                    check(Math.Abs(ParkingProximity.Measure(pose,new[]{post},front,side)-Math.Sqrt(.02))<1e-8,"Diagonal corner clearance is continuous outside the old ray fans");
                }
                var mirrorPost=Local(pose,side*(CarSpec.MirrorX+CarSpec.MirrorWidth/2+.03),CarSpec.MirrorZ);
                check(Math.Abs(ParkingProximity.Measure(pose,new[]{mirrorPost},true,side)-.02)<1e-8,"Mirror-only obstruction warns before body clearance is exhausted");
                var touching=Local(pose,side*(CarSpec.MirrorX+CarSpec.MirrorWidth/2+.005),CarSpec.MirrorZ);
                check(DrivingModel.Collides(pose,new[]{touching}) && Reading(pose,touching)<1e-8,"Mirror collision and proximity share the same envelope");
            }
            for(int step=1;step<=180;step++)
            {
                double clearance=step*.01;
                foreach(bool front in new[]{false,true})
                {
                    var wall=Local(pose,0,front?-CarSpec.Front-clearance-.05:CarSpec.Rear+clearance+.05,8,.10);
                    check(Math.Abs(ParkingProximity.Measure(pose,new[]{wall},front,0)-clearance)<1e-8,"Bumper distance changes continuously over the complete warning range");
                }
            }
        }
        // Independent rectangle-edge oracle: compare the closest of all displayed
        // regions with exact distance to the physical body and mirror footprints.
        double Distance(Footprint a,Footprint b)
        {
            if(a.Overlaps(b)) return 0;
            double min=double.MaxValue;var ac=a.Corners();var bc=b.Corners();
            void PointsToEdges((double X,double Z)[] points,(double X,double Z)[] edges)
            {
                foreach(var p in points) for(int i=0;i<4;i++)
                {
                    var v=edges[i];var w=edges[(i+1)%4];double dx=w.X-v.X,dz=w.Z-v.Z;
                    double t=Math.Clamp(((p.X-v.X)*dx+(p.Z-v.Z)*dz)/(dx*dx+dz*dz),0,1);
                    min=Math.Min(min,Math.Sqrt(Math.Pow(p.X-v.X-t*dx,2)+Math.Pow(p.Z-v.Z-t*dz,2)));
                }
            }
            PointsToEdges(ac,bc);PointsToEdges(bc,ac);return min;
        }
        var random=new Random(754);
        for(int i=0;i<4000;i++)
        {
            var pose=new Pose(random.NextDouble()*20-10,random.NextDouble()*20-10,random.NextDouble()*Math.PI*2);
            var obstacle=Local(pose,random.NextDouble()*8-4,random.NextDouble()*10-6,.02+random.NextDouble()*2,.02+random.NextDouble()*5,random.NextDouble()*Math.PI);
            double expected=Math.Min(2,Math.Min(Distance(DrivingModel.Body(pose),obstacle.Shape),Math.Min(Distance(DrivingModel.Mirror(pose,-1),obstacle.Shape),Distance(DrivingModel.Mirror(pose,1),obstacle.Shape))));
            check(Math.Abs(Reading(pose,obstacle)-expected)<1e-8,$"Rotated obstacle {i}: expected {expected:R}, actual {Reading(pose,obstacle):R}; pose {pose}; obstacle {obstacle.Shape}");
        }
        foreach(int side in new[]{-1,1})
        {
            var pose=new Pose(0,0,0);var post=Local(pose,side*1.02,CarSpec.Rear+.30,.04,.04);
            var car=new DrivingModel {Pose=pose,Gear=Gear.R,ParkingBrake=false,Speed=-.55,Steer=-side*.6};
            bool touched=false;
            for(int frame=0;frame<900;frame++)
            {
                double before=Reading(car.Pose,post);
                if(!car.Move(1.0/120,new[]{post}))
                {check(before<.015,"Reversing into a corner obstacle warns before swept collision stops the car");touched=true;break;}
            }
            check(touched,"Reverse-turn regression actually reaches the corner obstacle");
        }
    }
}
