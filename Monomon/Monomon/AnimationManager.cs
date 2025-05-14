namespace Monomon
{
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;

    internal class AnimationManager
    {
        int numFrames;
        int numColumns;
        Vector2 size;

        int counter;
        int activeFrame;
        int interval;

        int rowPos;
        int columnPos;
        
        // Store the starting position for the current animation
        private int startingRow;
        private int startingColumn;
        
        // Flag for single-frame animations
        private bool isStaticFrame;

        public AnimationManager(int numFrames, int numColumns, Vector2 size)
        {
            this.numFrames = numFrames;
            this.numColumns = numColumns;
            this.size = size;

            counter = 0;
            activeFrame = 0;
            interval = 30;
            
            startingRow = 0;
            startingColumn = 0;
            rowPos = 0;
            columnPos = 0;
            isStaticFrame = false;
        }

        public void Update()
        {
            // Skip update for static frames
            if (isStaticFrame)
                return;
                
            counter++;
            if (counter > interval)
            {
                counter = 0;
                NextFrame();
            }
        }

        public void NextFrame()
        {
            // Don't animate static frames
            if (isStaticFrame)
                return;
                
            activeFrame++;
            columnPos++;
            
            // Check if we need to move to the next row
            if (columnPos >= numColumns)
            {
                columnPos = 0;
                rowPos++;
            }
            
            // Check if we've reached the end of the animation
            if (activeFrame >= numFrames)
            {
                ResetAnimation();
            }
        }

        public void ResetAnimation()
        {
            activeFrame = 0;
            columnPos = startingColumn;
            rowPos = startingRow;
        }

        public Rectangle GetFrame()
        {
            return new Rectangle(
                (int)size.X * columnPos,
                (int)size.Y * rowPos,
                (int)size.X,
                (int)size.Y);
        }
        
        public void SetAnimation(int startFrame, int endFrame, int startRow)
        {
            // Check if this is a static frame (single frame animation)
            isStaticFrame = (startFrame == endFrame);
            
            // Store the animation parameters
            this.numFrames = endFrame - startFrame + 1;
            
            // Calculate starting column and row
            int framesPerRow = numColumns;
            this.startingColumn = startFrame % framesPerRow;
            this.startingRow = startRow;
            
            // Set current position to start
            this.rowPos = startingRow;
            this.columnPos = startingColumn;
            
            // Reset frame counter
            this.activeFrame = 0;
            this.counter = 0;
        }
        
        // Specific method for setting a static (non-animated) frame
        public void SetStaticFrame(int frameIndex, int row)
        {
            isStaticFrame = true;
            
            // Calculate column position based on frame index
            int column = frameIndex % numColumns;
            
            // Set frame position
            this.rowPos = row;
            this.columnPos = column;
            
            // Store starting position (not really needed but for consistency)
            this.startingRow = row;
            this.startingColumn = column;
            
            // Set numFrames to 1
            this.numFrames = 1;
        }
        
        public void SetAnimationSpeed(int newInterval)
        {
            interval = newInterval;
        }
    }
}