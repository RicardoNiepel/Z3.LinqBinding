using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Schema;
using Z3.LinqBinding;

namespace Z3.LinqBindingDemo
{

   /// <summary>
   /// This class solves the famous <see href="https://en.wikipedia.org/wiki/Missionaries_and_cannibals_problem">Missionaries and cannibals problem</see>
   /// "Three missionaries and three cannibals must cross a river using a boat which can carry at most two people, under the constraint that, for both banks, if there are missionaries present on the bank, they cannot be outnumbered by cannibals
   /// (if they were, the cannibals would eat the missionaries). The boat cannot cross the river by itself with no people on board.
   /// The class contains the state representation for the problem and embeds the rules into a Z3 solving theorem
   /// </summary>
   public class MissionariesAndCannibals
   {

      // The number of Missionaries and cannibals (3 in the original problem)
      public int NbMissionaries { get; set; } = 3;
      // The size of the boat (2 in the original problem)
      public int SizeBoat { get; set; } = 2;

      // length of the solution
      private int _length;

      //Property to access the length in Z3
      public int Length
      {
         get => _length;
         set
         {
            _length = value;
            // When length is computed by Z3, we initialize arrays to retrieve values
            Missionaries = new int[value];
            Cannibals = new int[value];
         }
      }

      // An array that contains the number of Missionaries on the starting bank at each step
      public int[] Missionaries { get; set; }
      // An array that contains the number of Cannibals on the starting bank at each step
      public int[] Cannibals { get; set; }

      /// <summary>
      /// An easy to read representation of the proposed solution
      /// </summary>
      /// <returns>A string where each line represents the environment state for a step</returns>
      public override string ToString()
      {
         var sb = new StringBuilder();
         for (int i = 0; i < Cannibals.Length; i++)
         {
            sb.AppendLine($"{i + 1} - (({Missionaries[i]}M, {Cannibals[i]}C, {1 - i % 2}), ({(i % 2)}, {NbMissionaries - Missionaries[i]}M, {NbMissionaries - Cannibals[i]}C))");
         }

         return sb.ToString();

      }

      /// <summary>
      /// Creates a theorem with the rules of the game, and the starting parameters initialized from this instance
      /// </summary>
      /// <param name="context">A wrapping Z3 context used to interpret c# Lambda into Z3 constraints</param>
      /// <returns>A typed theorem to be solved</returns>
      public Theorem<MissionariesAndCannibals> Create(Z3Context context)
      {
         var theorem = context.NewTheorem<MissionariesAndCannibals>();
         // We start with global constraints, to be injected into the lambda expression
         var sizeBoat = this.SizeBoat;
         int nbMissionaries = this.NbMissionaries;
         int maxlength = this.Length;

         // Initial state
         theorem = theorem.Where(caM => caM.NbMissionaries == nbMissionaries);
         theorem = theorem.Where(caM => caM.SizeBoat == sizeBoat);
         theorem = theorem.Where(caM => caM.Missionaries[0] == caM.NbMissionaries && caM.Cannibals[0] == caM.NbMissionaries);

         // Transition model: We filter each step according to legal moves
         for (int iclosure = 0; iclosure < maxlength; iclosure++)
         {
            var i = iclosure;
            //The 2 banks cannot have more people than the initial population
            theorem = theorem.Where(caM => caM.Cannibals[i] >= 0
                                           && caM.Cannibals[i] <= caM.NbMissionaries
                                           && caM.Missionaries[i] >= 0
                                           && caM.Missionaries[i] <= caM.NbMissionaries);
            if (i % 2 == 0)
            {
               // On even steps, the starting bank loses between 1 and SizeBoat people 
               theorem = theorem.Where(caM => caM.Cannibals[i + 1] <= caM.Cannibals[i]
                                              && caM.Missionaries[i + 1] <= caM.Missionaries[i]
                                              && caM.Cannibals[i + 1] + caM.Missionaries[i + 1] - caM.Cannibals[i] - caM.Missionaries[i] < 0
                                              && caM.Cannibals[i + 1] + caM.Missionaries[i + 1] - caM.Cannibals[i] - caM.Missionaries[i] >= -caM.SizeBoat);

            }
            else
            {
               // On odd steps, the starting bank gains between 1 and SizeBoat people
               theorem = theorem.Where(caM =>
                                         caM.Cannibals[i + 1] >= caM.Cannibals[i]
                                         && caM.Missionaries[i + 1] >= caM.Missionaries[i]
                                         && caM.Cannibals[i + 1] + caM.Missionaries[i + 1] - caM.Cannibals[i] - caM.Missionaries[i] > 0
                                         && caM.Cannibals[i + 1] + caM.Missionaries[i + 1] - caM.Cannibals[i] - caM.Missionaries[i] <= caM.SizeBoat);

            }

            //Never less missionaries than cannibals on any bank
            theorem = theorem.Where(caM => (caM.Missionaries[i] == 0 || (caM.Missionaries[i] >= caM.Cannibals[i]))
                                     && (caM.Missionaries[i] == caM.NbMissionaries || ((caM.NbMissionaries - caM.Missionaries[i]) >= (caM.NbMissionaries - caM.Cannibals[i]))));

         }

         // Goal state
         // When finished, No more people on the starting bank
         theorem = theorem.Where(
           caM => caM.Length > 0
                  && caM.Length < maxlength
                  && caM.Missionaries[caM.Length - 1] == 0
                  && caM.Cannibals[caM.Length - 1] == 0
         );


         return theorem;

      }

      /// <summary>
      /// Searches for a minimal solution from an upper bound through binary search
      /// </summary>
      /// <param name="entity">The template entity with global variable and the initial maximum size/Length</param>
      /// <returns>A minimal model if any, null otherwise</returns>
      public static MissionariesAndCannibals SearchMinimal(MissionariesAndCannibals entity)
      {
         return ShortestTheoremSearcher<MissionariesAndCannibals>.SearchMinimal(
           //Maximum size of solutions to explore
           entity.Length,
          //Creating the theorem for a given maximum size
          (context, mLength) => new MissionariesAndCannibals() { NbMissionaries = entity.NbMissionaries, SizeBoat = entity.SizeBoat, Length = mLength }.Create(context),
          //Retrieving the solution size
          can => can.Length);
      }


   }
}
