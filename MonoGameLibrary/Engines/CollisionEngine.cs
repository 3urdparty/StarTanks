using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace MonoGameLibrary
{
    public class CollisionEngine
    {
        public delegate void CollisionEventHandler(
            ICollidable thisCollidable,
            ICollidable thatCollidable
        );
        public event CollisionEventHandler OnCollision;

        private struct CollisionKey : IEquatable<CollisionKey>
        {
            public string FirstGroupName;
            public string SecondGroupName;

            public CollisionKey(string first, string second)
            {
                FirstGroupName = first;
                SecondGroupName = second;
            }

            public bool Equals(CollisionKey that)
            {
                if (FirstGroupName.Length * SecondGroupName.Length == 0)
                    return true;
                else
                    return (
                        FirstGroupName == that.FirstGroupName
                            && SecondGroupName == that.SecondGroupName
                        || FirstGroupName == that.SecondGroupName
                            && SecondGroupName == that.FirstGroupName
                    );
            }

            public override int GetHashCode()
            {
                return FirstGroupName.GetHashCode() ^ SecondGroupName.GetHashCode();
            }

            public override string ToString()
            {
                return $"Collision Key[{FirstGroupName}, {SecondGroupName}]";
            }
        }

        // Fields and Properties
        public bool Enabled { get; set; }

        // Stores which groups to detect collisions between
        private HashSet<CollisionKey> _collisionGroups;

        // Stores the collidables
        private Dictionary<string, HashSet<ICollidable>> _collidables;

        // Constructors and Methods
        public CollisionEngine()
        {
            Enabled = true;

            _collisionGroups = new HashSet<CollisionKey>();
            _collidables = new Dictionary<string, HashSet<ICollidable>>();
        }

        /// <summary>
        /// Register two groups for collision detection.
        /// </summary>
        public void Listen(string thisGroupName, string thatGroupName)
        {
            var key = new CollisionKey(thisGroupName, thatGroupName);
            _collisionGroups.Add(key);
        }

        /// <summary>
        /// Register two collidable objects' groups for collision detection.
        /// </summary>
        public void Listen(ICollidable thisCollidable, ICollidable thatCollidable)
        {
            Listen(thisCollidable.GetGroupName(), thatCollidable.GetGroupName());
        }

        /// <summary>
        /// Register two types' groups for collision detection.
        /// </summary>
        public void Listen(Type thisType, Type thatType)
        {
            Listen(thisType.Name, thatType.Name);
        }

        public void Update()
        {
            if (Enabled)
            {
                foreach (var collidable in _collidables) { }
                foreach (var collisionKey in _collisionGroups)
                {
                    // Detect collision iff both groups have at least 1 object
                    if (
                        _collidables.ContainsKey(collisionKey.FirstGroupName)
                        && _collidables.ContainsKey(collisionKey.SecondGroupName)
                    )
                    {
                        foreach (var thisCollidable in _collidables[collisionKey.FirstGroupName])
                        {
                            foreach (
                                var thatCollidable in _collidables[collisionKey.SecondGroupName]
                            )
                            {
                                DetectCollision(thisCollidable, thatCollidable);
                            }
                        }
                    }
                }
            }
        }

        public void RegisterCollidableObject(GameObject obj)
        {
            if (obj is ICollidable)
            {
                ICollidable collidable = (ICollidable)obj;
                string groupName = collidable.GetGroupName();

                if (!_collidables.ContainsKey(groupName))
                {
                    _collidables[groupName] = new HashSet<ICollidable>();
                }

                _collidables[groupName].Add(collidable);
                Debug.WriteLine(
                    $"[CollisionEngine]: Added COLLIDABLE object {obj.Name} to group {groupName}"
                );
            }
        }

        public void DeregisterCollidableObject(GameObject obj)
        {
            if (obj is ICollidable)
            {
                ICollidable collidable = (ICollidable)obj;
                string groupName = collidable.GetGroupName();

                if (_collidables.ContainsKey(groupName))
                {
                    _collidables[groupName].Remove(collidable);
                    Debug.WriteLine(
                        $"[CollisionEngine]: Removed COLLIDABLE object {obj.Name} from group {groupName}"
                    );
                }
            }
        }

        /// <summary>
        /// Detects collision between two objects using SAT and fires the OnCollision event if they collide.
        /// </summary>
        private void DetectCollision(ICollidable thisObject, ICollidable thatObject)
        {
            // Use SAT to detect collision
            if (thisObject.GetBounds().AABB().Intersects(thatObject.GetBounds().AABB()))
            {
                if (thisObject.GetBounds().Intersects(thatObject.GetBounds()))
                {
                    // Fire OnCollision event
                    OnCollision?.Invoke(thisObject, thatObject);

                    // Execute colliders' internal handler
                    CollisionInfo thisCollisionData = new CollisionInfo { Other = thatObject };
                    CollisionInfo thatCollisionData = new CollisionInfo { Other = thisObject };

                    thisObject.OnCollision(thisCollisionData);
                    thatObject.OnCollision(thatCollisionData);
                }
            }
        }
    }
}
