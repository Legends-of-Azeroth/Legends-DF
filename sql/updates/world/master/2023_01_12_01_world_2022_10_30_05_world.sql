-- Ghostlands Suncrown Village and Isle of Tribulations respawn from sniff createobject2 and improved movement
DELETE FROM `creature` WHERE `id` IN (16313,16357);
DELETE FROM `creature` WHERE `guid` IN (12457,12469,12474); -- 3 missing spawns
DELETE FROM `creature_addon` WHERE `guid` IN (81766,81767,81768,81769,81771,81772,81773,81774,81775,81776,81777,81778,81779,81780,81781,81783,81784,81786,81787,81788,81789,81790,81791,81792,81795,81798,81802,81803,81804,81805);
DELETE FROM `creature_addon` WHERE `guid` IN (81806,81807,81808,81809,81810,81811,81812,81813,81814,81815,81816,81817,81818,81819,81820,81821,81822,81823,81824,81825,81826,81827,81828,81829,82110,82111,82112,12457,12469,12474);
INSERT INTO `creature` (`guid`,`id`,`map`,`zoneId`,`areaId`,`spawnDifficulties`,`phaseId`,`modelid`,`equipment_id`,`position_x`,`position_y`,`position_z`,`orientation`,`spawntimesecs`,`wander_distance`,`currentwaypoint`,`curhealth`,`curmana`,`MovementType`,`npcflag`,`unit_flags`,`dynamicflags`,`ScriptName`,`VerifiedBuild`) VALUES
(81766, 16313, 530, 0, 0, '0', 0, 0, 0, 8045.23, -7285.9673, 141.19753, 4.222597122192382812, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81767, 16313, 530, 0, 0, '0', 0, 0, 0, 8047.971, -7315.4497, 141.55814, 1.364494681358337402, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81768, 16313, 530, 0, 0, '0', 0, 0, 0, 7943.367, -7388.2334, 142.48712, 3.677036762237548828, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81769, 16313, 530, 0, 0, '0', 0, 0, 0, 7947.7134, -7354.116, 142.76176, 4.914618968963623046, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81771, 16313, 530, 0, 0, '0', 0, 0, 0, 8049.752, -7549.5767, 150.15224, 1.507288813591003417, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81772, 16313, 530, 0, 0, '0', 0, 0, 0, 8053.758, -7511.7935, 150.82181, 0.983593106269836425, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81773, 16313, 530, 0, 0, '0', 0, 0, 0, 8049.288, -7578.174, 148.62862, 1.258004426956176757, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81774, 16313, 530, 0, 0, '0', 0, 0, 0, 8081.1104, -7591.1626, 149.2503, 3.104130983352661132, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81775, 16313, 530, 0, 0, '0', 0, 0, 0, 8115.0537, -7548.682, 173.07104, 1.374262452125549316, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81776, 16313, 530, 0, 0, '0', 0, 0, 0, 8116.2554, -7587.172, 160.12703, 5.308646202087402343, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81777, 16313, 530, 0, 0, '0', 0, 0, 0, 8120.1196, -7518.934, 168.00075, 3.149159193038940429, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81778, 16313, 530, 0, 0, '0', 0, 0, 0, 8149.155, -7485.593, 151.39754, 3.898998260498046875, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81779, 16313, 530, 0, 0, '0', 0, 0, 0, 8118.783, -7611.8984, 148.70592, 0.906582891941070556, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81780, 16313, 530, 0, 0, '0', 0, 0, 0, 8119.0034, -7483.1997, 153.47397, 2.642226457595825195, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81781, 16313, 530, 0, 0, '0', 0, 0, 0, 8145.6157, -7614.4966, 149.45955, 2.301557064056396484, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81783, 16313, 530, 0, 0, '0', 0, 0, 0, 8150.204, -7583.5693, 156.19228, 4.686991691589355468, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81784, 16313, 530, 0, 0, '0', 0, 0, 0, 8155.163, -7549.097, 156.29398, 0.795627057552337646, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81786, 16313, 530, 0, 0, '0', 0, 0, 0, 8149.278, -7514.165, 157.83986, 2.711774826049804687, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81787, 16313, 530, 0, 0, '0', 0, 0, 0, 8084.3022, -7487.1997, 150.96777, 2.151896953582763671, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81788, 16313, 530, 0, 0, '0', 0, 0, 0, 8050.1787, -7413.635, 147.04948, 5.355109214782714843, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81789, 16313, 530, 0, 0, '0', 0, 0, 0, 8019.5146, -7387.6196, 143.35431, 0.007975091226398944, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81790, 16313, 530, 0, 0, '0', 0, 0, 0, 8057.4673, -7371.387, 143.97952, 0.921935975551605224, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81791, 16313, 530, 0, 0, '0', 0, 0, 0, 8037.097, -7362.474, 144.15495, 4.267343997955322265, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81792, 16313, 530, 0, 0, '0', 0, 0, 0, 8084.085, -7386.243, 143.51062, 0.480680257081985473, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81795, 16313, 530, 0, 0, '0', 0, 0, 0, 8016.048, -7415.3506, 146.32059, 3.04085540771484375, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81798, 16313, 530, 0, 0, '0', 0, 0, 0, 8085.4106, -7349.3037, 140.41075, 1.29018402099609375, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81802, 16313, 530, 0, 0, '0', 0, 0, 0, 8080.5312, -7317.009, 142.25566, 1.856836676597595214, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81803, 16313, 530, 0, 0, '0', 0, 0, 0, 8037.8213, -7382.6606, 165.41771, 5.553741931915283203, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81804, 16313, 530, 0, 0, '0', 0, 0, 0, 8052.1313, -7384.1016, 165.17769, 3.334594964981079101, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81805, 16313, 530, 0, 0, '0', 0, 0, 0, 8086.9863, -7380.058, 164.30424, 6.280980110168457031, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81806, 16313, 530, 0, 0, '0', 0, 0, 0, 8058.329, -7364.76, 164.47891, 1.952818632125854492, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81807, 16313, 530, 0, 0, '0', 0, 0, 0, 8083.0146, -7252.842, 141.04585, 4.51193094253540039, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81808, 16313, 530, 0, 0, '0', 0, 0, 0, 8051.4897, -7244.594, 142.79373, 1.455305337905883789, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81809, 16313, 530, 0, 0, '0', 0, 0, 0, 8064.4443, -7226.681, 142.77878, 1.581316232681274414, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81810, 16313, 530, 0, 0, '0', 0, 0, 0, 8059.8994, -7235.2104, 142.77878, 1.696429610252380371, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81811, 16313, 530, 0, 0, '0', 0, 0, 0, 8048.194, -7215.254, 158.66579, 0.457142353057861328, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81812, 16313, 530, 0, 0, '0', 0, 0, 0, 8075.539, -7209.988, 158.665, 5.834308624267578125, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81813, 16313, 530, 0, 0, '0', 0, 0, 0, 8002.7007, -7216.4355, 140.88164, 0.245077401399612426, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81814, 16313, 530, 0, 0, '0', 0, 0, 0, 8064.885, -7227.9834, 158.66376, 5.772104740142822265, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81815, 16313, 530, 0, 0, '0', 0, 0, 0, 8080.6973, -7237.239, 158.66457, 2.20580911636352539, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81816, 16313, 530, 0, 0, '0', 0, 0, 0, 8019.0337, -7182.7383, 136.10959, 2.62692880630493164, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81817, 16313, 530, 0, 0, '0', 0, 0, 0, 7986.704, -7182.1206, 136.10928, 0.916203022003173828, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81818, 16313, 530, 0, 0, '0', 0, 0, 0, 8050.903, -7185.6304, 141.23912, 0.224479854106903076, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81819, 16313, 530, 0, 0, '0', 0, 0, 0, 8003.2197, -7356.0254, 140.66624, 0.265046238899230957, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81820, 16313, 530, 0, 0, '0', 0, 0, 0, 7932.935, -7280.28, 140.02618, 1.780235767364501953, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81821, 16313, 530, 0, 0, '0', 0, 0, 0, 7919.733, -7217.0396, 131.48665, 0.222675919532775878, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81822, 16313, 530, 0, 0, '0', 0, 0, 0, 7881.9863, -7250.483, 134.22247, 0.426802843809127807, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81823, 16313, 530, 0, 0, '0', 0, 0, 0, 7879.328, -7285.0244, 138.88948, 5.124333381652832031, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81824, 16313, 530, 0, 0, '0', 0, 0, 0, 7911.893, -7275.0146, 140.01123, 5.516409873962402343, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81825, 16313, 530, 0, 0, '0', 0, 0, 0, 7913.404, -7320.0376, 141.83772, 2.287100553512573242, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81826, 16313, 530, 0, 0, '0', 0, 0, 0, 7912.9194, -7351.9307, 144.84807, 0.670562922954559326, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81827, 16313, 530, 0, 0, '0', 0, 0, 0, 7919.2095, -7257.4404, 155.89677, 2.387856721878051757, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81828, 16313, 530, 0, 0, '0', 0, 0, 0, 7912.164, -7274.4907, 155.89624, 1.193374514579772949, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81829, 16313, 530, 0, 0, '0', 0, 0, 0, 7907.989, -7291.5615, 155.89781, 1.554324746131896972, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(82110, 16313, 530, 0, 0, '0', 0, 0, 0, 7891.922, -7269.1685, 155.8974, 2.36991286277770996, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(82111, 16313, 530, 0, 0, '0', 0, 0, 0, 7982.987, -7278.188, 142.57901, 6.258038043975830078, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(82112, 16313, 530, 0, 0, '0', 0, 0, 0, 7978.2153, -7314.3267, 142.7645, 5.090083599090576171, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(12457, 16313, 530, 0, 0, '0', 0, 0, 0, 7971.8804, -7380.269, 141.32101, 0.689665198326110839, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(12469, 16313, 530, 0, 0, '0', 0, 0, 0, 8019.029, -7278.618, 141.91353, 0.881062269210815429, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(12474, 16313, 530, 0, 0, '0', 0, 0, 0, 8017.7163, -7319.009, 143.38083, 5.672662258148193359, 300, 0, 0, 1, 0, 0, 0, 0, 0, '', 0),
(81785, 16357, 530, 0, 0, '0', 0, 0, 0, 8022.0244, -7250.6743, 140.63289, 2.715512275695800781, 300, 0, 0, 1, 0, 2, 0, 0, 0, '', 0);

UPDATE `creature` SET `wander_distance`=5, `MovementType`=1 WHERE `id`=16313;

-- Pathing for Nerubis Guard Entry: 16313
SET @NPC := 81803;
SET @PATH := @NPC * 10;
UPDATE `creature` SET `wander_distance`=0,`MovementType`=2 WHERE `guid`=@NPC;
DELETE FROM `creature_addon` WHERE `guid`=@NPC;
INSERT INTO `creature_addon` (`guid`,`path_id`,`mount`,`bytes1`,`bytes2`,`emote`,`visibilityDistanceType`,`auras`) VALUES (@NPC,@PATH,0,0,1,0,0, '');
DELETE FROM `waypoint_data` WHERE `id`=@PATH;
INSERT INTO `waypoint_data` (`id`,`point`,`position_x`,`position_y`,`position_z`,`orientation`,`delay`,`move_type`,`action`,`action_chance`,`wpguid`) VALUES
(@PATH,1,8033.643,-7371.144,165.0797,NULL,0,0,0,100,0),
(@PATH,2,8040.8535,-7353.647,164.41042,NULL,0,0,0,100,0),
(@PATH,3,8056.7363,-7345.7485,163.84886,NULL,0,0,0,100,0),
(@PATH,4,8074,-7351.9585,163.66876,NULL,0,0,0,100,0),
(@PATH,5,8081.6147,-7362.401,163.82063,NULL,0,0,0,100,0),
(@PATH,6,8082.8716,-7378.999,164.28584,NULL,0,0,0,100,0),
(@PATH,7,8089.9097,-7380.9316,164.1907,NULL,0,0,0,100,0),
(@PATH,8,8094.854,-7362.633,159.5596,NULL,0,0,0,100,0),
(@PATH,9,8088.7847,-7351.132,155.29066,NULL,0,0,0,100,0),
(@PATH,10,8076.145,-7342.5244,152.84624,NULL,0,0,0,100,0),
(@PATH,11,8070.358,-7342.4834,152.83412,NULL,0,0,0,100,0),
(@PATH,12,8059.011,-7370.053,153.78862,NULL,0,0,0,100,0),
(@PATH,13,8051.393,-7386.35,152.3984,NULL,0,0,0,100,0),
(@PATH,14,8041.9385,-7383.407,149.58453,NULL,0,0,0,100,0),
(@PATH,15,8038.7256,-7376.1484,145.88435,NULL,0,0,0,100,0),
(@PATH,16,8040.108,-7364.4365,144.06822,NULL,0,0,0,100,0),
(@PATH,17,8020.9272,-7354.7837,140.86281,NULL,0,0,0,100,0),
(@PATH,18,8040.108,-7364.4365,144.06822,NULL,0,0,0,100,0),
(@PATH,19,8038.7256,-7376.1484,145.88435,NULL,0,0,0,100,0),
(@PATH,20,8041.9385,-7383.407,149.58453,NULL,0,0,0,100,0),
(@PATH,21,8051.393,-7386.35,152.3984,NULL,0,0,0,100,0),
(@PATH,22,8059.011,-7370.053,153.78862,NULL,0,0,0,100,0),
(@PATH,23,8070.358,-7342.4834,152.83412,NULL,0,0,0,100,0),
(@PATH,24,8076.145,-7342.5244,152.84624,NULL,0,0,0,100,0),
(@PATH,25,8088.7847,-7351.132,155.29066,NULL,0,0,0,100,0),
(@PATH,26,8094.854,-7362.633,159.5596,NULL,0,0,0,100,0),
(@PATH,27,8089.9097,-7380.9316,164.1907,NULL,0,0,0,100,0),
(@PATH,28,8082.8716,-7378.999,164.28584,NULL,0,0,0,100,0),
(@PATH,29,8081.6147,-7362.401,163.82063,NULL,0,0,0,100,0),
(@PATH,30,8074,-7351.9585,163.66876,NULL,0,0,0,100,0),
(@PATH,31,8056.7363,-7345.7485,163.84886,NULL,0,0,0,100,0),
(@PATH,32,8040.8535,-7353.647,164.41042,NULL,0,0,0,100,0),
(@PATH,33,8033.643,-7371.144,165.0797,NULL,0,0,0,100,0),
(@PATH,34,8038.7603,-7383.5,165.34204,NULL,0,0,0,100,0);

-- Pathing for Nerubis Guard Entry: 16313
SET @NPC := 81810;
SET @PATH := @NPC * 10;
UPDATE `creature` SET `wander_distance`=0,`MovementType`=2 WHERE `guid`=@NPC;
DELETE FROM `creature_addon` WHERE `guid`=@NPC;
INSERT INTO `creature_addon` (`guid`,`path_id`,`mount`,`bytes1`,`bytes2`,`emote`,`visibilityDistanceType`,`auras`) VALUES (@NPC,@PATH,0,0,1,0,0, '');
DELETE FROM `waypoint_data` WHERE `id`=@PATH;
INSERT INTO `waypoint_data` (`id`,`point`,`position_x`,`position_y`,`position_z`,`orientation`,`delay`,`move_type`,`action`,`action_chance`,`wpguid`) VALUES
(@PATH,1,8059.7866,-7234.3174,142.74286,NULL,0,0,0,100,0),
(@PATH,2,8043.001,-7257.784,140.44353,NULL,0,0,0,100,0),
(@PATH,3,8031.8647,-7253.7134,140.51013,NULL,0,0,0,100,0),
(@PATH,4,8028.828,-7227.35,139.8902,NULL,0,0,0,100,0),
(@PATH,5,8045.1406,-7206.0024,149.90591,NULL,0,0,0,100,0),
(@PATH,6,8054.3125,-7201.0605,153.15536,NULL,0,0,0,100,0),
(@PATH,7,8062.401,-7201.203,156.12938,NULL,0,0,0,100,0),
(@PATH,8,8075.4395,-7211.2393,158.62915,NULL,0,0,0,100,0),
(@PATH,9,8056.79,-7237.8306,158.63235,NULL,0,0,0,100,0),
(@PATH,10,8075.4395,-7211.2393,158.62915,NULL,0,0,0,100,0),
(@PATH,11,8062.401,-7201.203,156.12938,NULL,0,0,0,100,0),
(@PATH,12,8054.413,-7201.0063,153.1906,NULL,0,0,0,100,0),
(@PATH,13,8045.1406,-7206.0024,149.90591,NULL,0,0,0,100,0),
(@PATH,14,8028.828,-7227.35,139.8902,NULL,0,0,0,100,0),
(@PATH,15,8031.8647,-7253.7134,140.51013,NULL,0,0,0,100,0),
(@PATH,16,8043.001,-7257.784,140.44353,NULL,0,0,0,100,0);

-- Pathing for Nerubis Guard Entry: 16313
SET @NPC := 81819;
SET @PATH := @NPC * 10;
UPDATE `creature` SET `wander_distance`=0,`MovementType`=2 WHERE `guid`=@NPC;
DELETE FROM `creature_addon` WHERE `guid`=@NPC;
INSERT INTO `creature_addon` (`guid`,`path_id`,`mount`,`bytes1`,`bytes2`,`emote`,`visibilityDistanceType`,`auras`) VALUES (@NPC,@PATH,0,0,1,0,0, '');
DELETE FROM `waypoint_data` WHERE `id`=@PATH;
INSERT INTO `waypoint_data` (`id`,`point`,`position_x`,`position_y`,`position_z`,`orientation`,`delay`,`move_type`,`action`,`action_chance`,`wpguid`) VALUES
(@PATH,1,8005.139,-7355.5044,140.77748,NULL,0,0,0,100,0),
(@PATH,2,8022.629,-7351.25,141.15248,NULL,0,0,0,100,0),
(@PATH,3,8037.9766,-7343.384,141.1213,NULL,0,0,0,100,0),
(@PATH,4,8048.9287,-7325.928,141.03996,NULL,0,0,0,100,0),
(@PATH,5,8059.8706,-7305.5493,141.4163,NULL,0,0,0,100,0),
(@PATH,6,8057.078,-7277.6445,140.63231,NULL,0,0,0,100,0),
(@PATH,7,8044.9673,-7262.834,140.56853,NULL,0,0,0,100,0),
(@PATH,8,8018.8813,-7248.543,140.63513,NULL,0,0,0,100,0),
(@PATH,9,8002.0835,-7244.932,140.01013,NULL,0,0,0,100,0),
(@PATH,10,7979.1963,-7250.8296,137.36232,NULL,0,0,0,100,0),
(@PATH,11,7959.9375,-7262.9375,137.16505,NULL,0,0,0,100,0),
(@PATH,12,7951.943,-7287.8804,138.21864,NULL,0,0,0,100,0),
(@PATH,13,7950.151,-7316.854,140.35782,NULL,0,0,0,100,0),
(@PATH,14,7956.857,-7334.874,140.92525,NULL,0,0,0,100,0),
(@PATH,15,7971.9644,-7348.172,139.4391,NULL,0,0,0,100,0);

-- Pathing for Nerubis Guard Entry: 16313
SET @NPC := 81820;
SET @PATH := @NPC * 10;
UPDATE `creature` SET `wander_distance`=0,`MovementType`=2 WHERE `guid`=@NPC;
DELETE FROM `creature_addon` WHERE `guid`=@NPC;
INSERT INTO `creature_addon` (`guid`,`path_id`,`mount`,`bytes1`,`bytes2`,`emote`,`visibilityDistanceType`,`auras`) VALUES (@NPC,@PATH,0,0,1,0,0, '');
DELETE FROM `waypoint_data` WHERE `id`=@PATH;
INSERT INTO `waypoint_data` (`id`,`point`,`position_x`,`position_y`,`position_z`,`orientation`,`delay`,`move_type`,`action`,`action_chance`,`wpguid`) VALUES
(@PATH,1,7918.5625,-7276.8574,139.9279,NULL,0,0,0,100,0),
(@PATH,2,7950.742,-7287.3726,138.23781,NULL,0,0,0,100,0),
(@PATH,3,7948.3926,-7299.041,139.3423,NULL,0,0,0,100,0),
(@PATH,4,7930.652,-7307.738,139.65376,NULL,0,0,0,100,0),
(@PATH,5,7904.759,-7302.8438,144.49107,NULL,0,0,0,100,0),
(@PATH,6,7890.218,-7292.889,149.99257,NULL,0,0,0,100,0),
(@PATH,7,7889.828,-7277.808,155.13405,NULL,0,0,0,100,0),
(@PATH,8,7892.1943,-7269.5376,155.81406,NULL,0,0,0,100,0),
(@PATH,9,7912.4756,-7274.6274,155.82387,NULL,0,0,0,100,0),
(@PATH,10,7924.9443,-7278.246,155.81685,NULL,0,0,0,100,0),
(@PATH,11,7912.4756,-7274.6274,155.82387,NULL,0,0,0,100,0),
(@PATH,12,7892.1943,-7269.5376,155.81406,NULL,0,0,0,100,0),
(@PATH,13,7889.8325,-7277.7925,155.13788,NULL,0,0,0,100,0),
(@PATH,14,7890.218,-7292.889,149.99257,NULL,0,0,0,100,0),
(@PATH,15,7904.759,-7302.8438,144.49107,NULL,0,0,0,100,0),
(@PATH,16,7930.652,-7307.738,139.65376,NULL,0,0,0,100,0),
(@PATH,17,7948.3926,-7299.041,139.3423,NULL,0,0,0,100,0),
(@PATH,18,7950.742,-7287.3726,138.23781,NULL,0,0,0,100,0);

-- Pathing for Anok'suten Entry: 16357
SET @NPC := 81785;
SET @PATH := @NPC * 10;
DELETE FROM `creature_addon` WHERE `guid`=@NPC;
INSERT INTO `creature_addon` (`guid`,`path_id`,`mount`,`bytes1`,`bytes2`,`emote`,`visibilityDistanceType`,`auras`) VALUES (@NPC,@PATH,0,0,1,0,0, '');
DELETE FROM `waypoint_data` WHERE `id`=@PATH;
INSERT INTO `waypoint_data` (`id`,`point`,`position_x`,`position_y`,`position_z`,`orientation`,`delay`,`move_type`,`action`,`action_chance`,`wpguid`) VALUES
(@PATH,1,8019.065,-7249.331,140.63513,NULL,0,0,0,100,0),
(@PATH,2,7989.3853,-7246.862,138.67287,NULL,0,0,0,100,0),
(@PATH,3,7973.8228,-7252.6577,137.25917,NULL,0,0,0,100,0),
(@PATH,4,7962.366,-7261.485,137.04005,NULL,0,0,0,100,0),
(@PATH,5,7953.876,-7280.6797,137.76442,NULL,0,0,0,100,0),
(@PATH,6,7918.577,-7304.7744,140.1208,NULL,0,0,0,100,0),
(@PATH,7,7904.5815,-7301.776,144.68105,NULL,0,0,0,100,0),
(@PATH,8,7894.837,-7297.364,147.89558,NULL,0,0,0,100,0),
(@PATH,9,7890.0625,-7290.992,150.63379,NULL,0,0,0,100,0),
(@PATH,10,7889.0244,-7283.2954,153.71674,NULL,0,0,0,100,0),
(@PATH,11,7890.96,-7275.3027,155.81384,NULL,0,0,0,100,0),
(@PATH,12,7900.647,-7265.4077,155.81778,NULL,0,0,0,100,0),
(@PATH,13,7909.0566,-7261.003,155.81693,NULL,0,0,0,100,0),
(@PATH,14,7920.6763,-7263.211,155.81726,NULL,0,0,0,100,0),
(@PATH,15,7925.768,-7272.3706,155.81432,NULL,0,0,0,100,0),
(@PATH,16,7923.9473,-7281.567,155.81458,NULL,0,0,0,100,0),
(@PATH,17,7913.2866,-7288.8774,155.8172,NULL,0,0,0,100,0),
(@PATH,18,7902.268,-7284.6,155.81781,NULL,0,0,0,100,0),
(@PATH,19,7896.721,-7274.737,155.81569,NULL,0,0,0,100,0),
(@PATH,20,7892.6104,-7273.857,155.81433,NULL,0,0,0,100,0),
(@PATH,21,7889.3013,-7277.143,155.2871,NULL,0,0,0,100,0),
(@PATH,22,7888.722,-7288.2344,151.63979,NULL,0,0,0,100,0),
(@PATH,23,7891.923,-7295.921,148.78302,NULL,0,0,0,100,0),
(@PATH,24,7908.8823,-7303.67,142.74924,NULL,0,0,0,100,0),
(@PATH,25,7930.0903,-7317.6685,141.22017,NULL,0,0,0,100,0),
(@PATH,26,7955.0176,-7333.0054,140.89871,NULL,0,0,0,100,0),
(@PATH,27,7965.2476,-7344.0376,140.29329,NULL,0,0,0,100,0),
(@PATH,28,7984.58,-7354.614,138.95168,NULL,0,0,0,100,0),
(@PATH,29,8021.0947,-7357.2993,141.18483,NULL,0,0,0,100,0),
(@PATH,30,8031.5723,-7361.1997,144.14993,NULL,0,0,0,100,0),
(@PATH,31,8035.413,-7367.5054,144.25429,NULL,0,0,0,100,0),
(@PATH,32,8040.828,-7367.184,144.13156,NULL,0,0,0,100,0),
(@PATH,33,8042.856,-7361.6475,143.927,NULL,0,0,0,100,0),
(@PATH,34,8038.6978,-7358.2466,143.91435,NULL,0,0,0,100,0),
(@PATH,35,8033.383,-7360.155,144.08145,NULL,0,0,0,100,0),
(@PATH,36,8028.712,-7359.1294,143.39352,NULL,0,0,0,100,0),
(@PATH,37,8023.759,-7356.303,141.31606,NULL,0,0,0,100,0),
(@PATH,38,8023.71,-7353.0522,141.3102,NULL,0,0,0,100,0),
(@PATH,39,8039.088,-7342.489,141.1213,NULL,0,0,0,100,0),
(@PATH,40,8052.1616,-7325.52,141.14775,NULL,0,0,0,100,0),
(@PATH,41,8057.1235,-7312.013,141.27275,NULL,0,0,0,100,0),
(@PATH,42,8058.7734,-7296.549,141.38231,NULL,0,0,0,100,0),
(@PATH,43,8056.56,-7279.0728,140.63231,NULL,0,0,0,100,0),
(@PATH,44,8052.099,-7267.522,140.50731,NULL,0,0,0,100,0),
(@PATH,45,8044.862,-7261.3804,140.44353,NULL,0,0,0,100,0),
(@PATH,46,8044.366,-7256.9663,140.44353,NULL,0,0,0,100,0),
(@PATH,47,8048.0747,-7251.486,142.7578,NULL,0,0,0,100,0),
(@PATH,48,8057.78,-7236.897,142.74286,NULL,0,0,0,100,0),
(@PATH,49,8062.8853,-7235.33,142.74286,NULL,0,0,0,100,0),
(@PATH,50,8070.741,-7231.8384,142.74284,NULL,0,0,0,100,0),
(@PATH,51,8070.8423,-7224.639,142.74284,NULL,0,0,0,100,0),
(@PATH,52,8064.472,-7219.7246,142.74284,NULL,0,0,0,100,0),
(@PATH,53,8057.474,-7223.617,142.74286,NULL,0,0,0,100,0),
(@PATH,54,8056.8037,-7228.8247,142.74286,NULL,0,0,0,100,0),
(@PATH,55,8058.48,-7235.7656,142.74286,NULL,0,0,0,100,0),
(@PATH,56,8053.2397,-7243.334,142.75194,NULL,0,0,0,100,0),
(@PATH,57,8047.385,-7251.423,142.75778,NULL,0,0,0,100,0),
(@PATH,58,8042.708,-7257.291,140.44353,NULL,0,0,0,100,0),
(@PATH,59,8039.7646,-7258.069,140.44353,NULL,0,0,0,100,0),
(@PATH,60,8029.763,-7253.808,140.51013,NULL,0,0,0,100,0);
