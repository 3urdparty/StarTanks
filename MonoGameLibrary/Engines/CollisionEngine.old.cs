 // using System;
 // using System.Collections.Generic;
 // using System.Diagnostics;
 //
 // namespace MonoGameLibrary
 // {
 //     public class CollisionEngine
 //     {
 //         public delegate bool CollisionDetector(ICollidable thisCollidable, ICollidable thatCollidable);
 //         public delegate void CollisionHandler(ICollidable thisCollidable, ICollidable thatCollidable);
 //
 //         // Structs
 //         private struct CollisionKey : IEquatable<CollisionKey>
 //         {
 //             public string FirstGroupName;
 //             public string SecondGroupName;
 //
 //             public CollisionKey(string first, string second)
 //             {
 //                 FirstGroupName = first;
 //                 SecondGroupName = second;
 //             }
 //
 //             public bool Equals(CollisionKey that)
 //             {
 //                 // Prevents empty strings from being inserted
 //                 if (FirstGroupName.Length * SecondGroupName.Length == 0)
 //                     return true;
 //                 else
 //                     return (FirstGroupName == that.FirstGroupName &&
 //                             SecondGroupName == that.SecondGroupName ||
 //                             FirstGroupName == that.SecondGroupName &&
 //                             SecondGroupName == that.FirstGroupName);
 //             }
 //
 //             public override int GetHashCode()
 //             {
 //                 return FirstGroupName.GetHashCode() ^ SecondGroupName.GetHashCode();
 //             }
 //
 //             public override string ToString()
 //             {
 //                 return $"Collision Key[{FirstGroupName}, {SecondGroupName}]";
 //             }
 //         }
 //
 //         // Fields and Properties
 //         public bool Enabled { get; set; }
 //
 //         // Stores which group to detect collisions
 //         private HashSet<CollisionKey> _collisionGroups;
 //
 //         // Stores the collidables
 //         private Dictionary<string, HashSet<ICollidable>> _collidables;
 //         private Dictionary<CollisionKey, CollisionDetector> _collisionDetectors;
 //         private Dictionary<CollisionKey, CollisionHandler> _collisionHandlers;
 //
 //         // Constructors and Methods
 //         public CollisionEngine()
 //         {
 //             Enabled = true;
 //
 //             _collisionGroups = new HashSet<CollisionKey>();
 //             _collisionDetectors = new Dictionary<CollisionKey, CollisionDetector>();
 //             _collisionHandlers = new Dictionary<CollisionKey, CollisionHandler>();
 //             _collidables = new Dictionary<string, HashSet<ICollidable>>();
 //
 //             // [COMMENTED TEMPORARILY FOR PONG GAME]
 //             // Add listener to handle object addition and removal
 //             // GameObjectCollection.ObjectAdded += RegisterCollidableObject;
 //             // GameObjectCollection.ObjectRemoved += DeregisterCollidableObject;
 //         }
 //
 //         // NOTE: Listen should be called after all game objects has been initialized
 //         public void Listen(string thisName, string thatName
 //                           , CollisionDetector detector, CollisionHandler handler = null)
 //         {
 //             var key = new CollisionKey(thisName, thatName);
 //             if (_collisionGroups.Add(key))
 //             {
 //                 // Add collision detector (this is the first one)
 //                 _collisionDetectors.Add(key, detector);
 //
 //                 if (handler != null)
 //                 {
 //                     _collisionHandlers.Add(key, handler);
 //                 }
 //             }
 //             else
 //             {
 //                 // Prevent duplicated collision detector and handler added
 //                 _collisionDetectors[key] -= detector;
 //                 _collisionDetectors[key] += detector;
 //
 //                 if (handler != null)
 //                 {
 //                     _collisionHandlers[key] -= handler;
 //                     _collisionHandlers[key] += handler;
 //                 }
 //             }
 //         }
 //
 //         // NOTE: Listen should be called after all game objects has been initialized
 //         public void Listen(ICollidable thisCollidable, ICollidable thatCollidable
 //                           , CollisionDetector detector, CollisionHandler handler = null)
 //         {
 //             Listen(thisCollidable.GetGroupName(), thatCollidable.GetGroupName(), detector, handler);
 //         }
 //
 //         public void Listen(Type thisType, Type thatType
 //                           , CollisionDetector detector, CollisionHandler handler = null)
 //         {
 //             Listen(thisType.Name, thatType.Name, detector, handler);
 //         }
 //
 //         public void Update()
 //         {
 //             if (Enabled)
 //             {
 //                 foreach (var collisionKey in _collisionGroups)
 //                 {
 //                     // Detect collision iff both names has at least 1 object.
 //                     if (_collidables.ContainsKey(collisionKey.FirstGroupName) &&
 //                         _collidables.ContainsKey(collisionKey.SecondGroupName))
 //                     {
 //                         foreach (var thisCollidable in _collidables[collisionKey.FirstGroupName])
 //                         {
 //                             foreach (var thatCollidable in _collidables[collisionKey.SecondGroupName])
 //                             {
 //                                 DetectCollision(collisionKey, thisCollidable, thatCollidable);
 //                             }
 //                         }
 //                     }
 //                 }
 //             }
 //         }
 //
 //         public void RegisterCollidableObject(GameObject obj)
 //         {
 //             if (obj is ICollidable)
 //             {
 //                 ICollidable collidable = (ICollidable)obj;
 //                 string groupName = collidable.GetGroupName();
 //
 //                 if (!_collidables.ContainsKey(groupName))
 //                 {
 //                     _collidables[groupName] = new HashSet<ICollidable>();
 //                 }
 //
 //                 _collidables[groupName].Add(collidable);
 //                 Debug.WriteLine($"[CollisionEngine]: Added COLLIDABLE object {obj.Name} to group {groupName}");
 //             }
 //         }
 //
 //         public void DeregisterCollidableObject(GameObject obj)
 //         {
 //             if (obj is ICollidable)
 //             {
 //                 ICollidable collidable = (ICollidable)obj;
 //                 string groupName = collidable.GetGroupName();
 //
 //                 if (_collidables.ContainsKey(groupName))
 //                 {
 //                     _collidables[groupName].Remove((ICollidable)obj);
 //                     Debug.WriteLine($"[CollisionEngine]: Removed COLLIDABLE object {obj.Name} from group {groupName}");
 //                 }
 //             }
 //         }
 //
 //         private void DetectCollision(CollisionKey collisionKey, ICollidable thisObject, ICollidable thatObject)
 //         {
 //             var collisionDetector = _collisionDetectors[collisionKey];
 //
 //             if (collisionDetector(thisObject, thatObject))
 //             {
 //                 CollisionInfo thisCollisionData = new CollisionInfo();
 //                 CollisionInfo thatCollisionData = new CollisionInfo();
 //
 //                 thisCollisionData.Other = thatObject;
 //                 thatCollisionData.Other = thisObject;
 //
 //                 // Execute external collision handlers if exists
 //                 if (_collisionHandlers.ContainsKey(collisionKey))
 //                 {
 //                     _collisionHandlers[collisionKey](thisObject, thatObject);
 //                 }
 //
 //                 // Execute colliders' internal handler
 //                 thisObject.OnCollision(thisCollisionData);
 //                 thatObject.OnCollision(thatCollisionData);
 //             }
 //         }
 //
 //         #region Collision Detectors
 //
 //         public static bool AABB(ICollidable thisCollidable, ICollidable thatCollidable)
 //         {
 //             // return thisCollidable.GetBound().Intersects(thatCollidable.GetBound());
 //             return thisCollidable.GetBound().Left <= thatCollidable.GetBound().Right &&
 //                    thatCollidable.GetBound().Left <= thisCollidable.GetBound().Right &&
 //                    thisCollidable.GetBound().Top <= thatCollidable.GetBound().Bottom &&
 //                    thatCollidable.GetBound().Top <= thisCollidable.GetBound().Bottom;
 //         }
 //
 //         public static bool NotAABB(ICollidable thisCollidable, ICollidable thatCollidable)
 //         {
 //             // TODO: Technically, this is one not inside another (can still intersect)
 //             //bool thisHasThat = thisCollidable.GetBound().Contains(thatCollidable.GetBound());
 //             //bool thatHasThis = thatCollidable.GetBound().Contains(thisCollidable.GetBound());
 //             //return !(thisHasThat || thatHasThis);
 //             return !AABB(thisCollidable, thatCollidable);
 //         }
 //
 //         public static bool OneInsideAnother(ICollidable thisCollidable, ICollidable thatCollidable)
 //         {
 //             bool thisContainsThat = thisCollidable.GetBound().Contains(thatCollidable.GetBound());
 //             bool thatContainsThis = thatCollidable.GetBound().Contains(thisCollidable.GetBound());
 //             return (thisContainsThat || thatContainsThis);
 //         }
 //
 //         public static bool NotOneInsideAnother(ICollidable thisCollidable, ICollidable thatCollidable)
 //         {
 //             return !OneInsideAnother(thisCollidable, thatCollidable);
 //         }
 //
 //         #endregion
 //     }
 // }
