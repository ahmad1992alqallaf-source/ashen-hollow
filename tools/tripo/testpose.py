# raise both upper arms toward the T-pose in world space, bend the right knee forward
for side, s in (('l', 1), ('r', -1)):
    aim('upperarm_' + side, (s * 1.0, 0.0, -0.25))
pb = arm.pose.bones['calf_r']; bpy.context.view_layer.update()
T = mathutils.Matrix.Translation(pb.head); R = mathutils.Matrix.Rotation(math.radians(45), 4, 'X')
pb.matrix = T @ R @ T.inverted() @ pb.matrix; bpy.context.view_layer.update()
